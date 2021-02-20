using BridgeClient.DataModel;
using BridgeClient.Extensions;
using Microsoft.FlightSimulator.SimConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace BridgeClient
{
    class SimConnectViewModel : BaseViewModel
    {
        public static SimConnectViewModel Instance;

        public bool IsConnected { get => Get<bool>(); set => Set(value); }
        public string Title => All.ContainsKey("TITLE") ? (string)All["TITLE"] : null;

        public FpsCounter BridgeCounter { get; }

        private SimConnector m_simConnect;
        public Dictionary<string, object> All = new Dictionary<string, object>();
        public Dictionary<string, WSValue> m_localVars = new Dictionary<string, WSValue>();
        public Dictionary<string, WSValue> m_aircraftVars = new Dictionary<string, WSValue>();
        private List<string> m_localVarNames = new List<string>();
        private object m_allLock = new object();
        private object m_writeLock = new object();
        private int m_lastCommandId;
        private List<WSValue> m_pendingWrites = new List<WSValue>();
        private Queue<WSValue> m_pendingAircraftVarRequests = new Queue<WSValue>();
        private Dictionary<uint, WSValue> m_dataRequests = new Dictionary<uint, WSValue>();

        private Dictionary<string, Enum> m_events = new Dictionary<string, Enum>();
        private uint m_lastEvent = 0;

        private uint m_lastDataRequestId;
        private uint m_lastDefineId;

        private bool m_isWaitingForReply = false;


        public SimConnectViewModel()
        {
            BridgeCounter = new FpsCounter();
            m_lastCommandId = new Random().Next(0, 500);
            Instance = this;
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                var t = new Thread(SimConnectThreadProc);
                t.IsBackground = true;
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            });

            m_pendingAircraftVarRequests.Enqueue(new WSValue { name = "TITLE", unit = "string" });
        }

        void SimConnectThreadProc()
        {
            m_simConnect = new SimConnector();
            m_simConnect.Connected += (isConnected) => IsConnected = isConnected;
            m_simConnect.Connect(() =>
            {
                m_simConnect.m_simConnect.OnRecvClientData += OnRecvClientData;
                m_simConnect.m_simConnect.OnRecvSimobjectData += OnRecvSimobjectData;

                m_simConnect.m_simConnect.MapClientDataNameToID("BRIDGE_WriteToSim", ClientData.WriteToSim);
                m_simConnect.m_simConnect.MapClientDataNameToID("BRIDGE_ReadFromSim", ClientData.ReadFromSim);
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.WriteToSim, 0, (uint)Marshal.SizeOf(typeof(WriteToSim)), 0, 0);
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.ReadFromSim, 0, (uint)Marshal.SizeOf(typeof(ReadFromSim)), 0, 0);

                m_simConnect.m_simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ReadFromSim>(ClientData.ReadFromSim);
                m_simConnect.m_simConnect.RequestClientData(
                    ClientData.ReadFromSim,
                    DATA_REQUESTS.ReadFromSimChanged,
                    ClientData.ReadFromSim, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);

                // Begin streaming A:Vars
                while (m_pendingAircraftVarRequests.Any())
                {
                    RequestSimData(m_pendingAircraftVarRequests.Dequeue());
                }

                // Begin streaming L:vars
                OnSimWriteCompleted();
            });

            Dispatcher.Run();
        }

        private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var value = m_dataRequests[data.dwRequestID];
            if (data.dwData[0] is SimDataString256 simData)
            {
                value.value = simData.value;
            }
            else if (data.dwData[0] is SimDataDouble simDouble)
            {
                value.value = simDouble.value;
            }
            else throw new NotImplementedException();

            WriteAllValue(value);
        }

        private void WriteAllValue(WSValue value)
        {
            All[value.name] = value.value;

            SimpleHTTPServer.TakeOperation(new WSValue[] { value });
        }

        public KeyValuePair<string, object>[] GetAllSafe()
        {
            lock (m_allLock)
            {
                return All.ToArray();
            }
        }

        private void OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA recvData)
        {
            var data = (ReadFromSim)(recvData.dwData[0]);
            //  Trace.WriteLine($"OnRecvClientData {data.lastCommandId}");
            var ret = new List<WSValue>();
            lock (m_allLock)
            {
                for (var i = 0; i < Math.Min(data.valueCount, m_localVarNames.Count); i++)
                {
                    All[m_localVarNames[i]] = data.data[i];

                    var wsValue = m_localVars[m_localVarNames[i]];
                    wsValue.value = data.data[i];
                    ret.Add(wsValue);
                }
            }
            SimpleHTTPServer.TakeOperation(ret.ToArray());


            BridgeCounter.GotFrame();

            if (data.lastCommandId == m_lastCommandId)
            {
                m_isWaitingForReply = false;
                MaybeTriggerWriteToSim();
            }
            else
            {
                // Trace.WriteLine("Got unexpectyed " + data.lastCommandId);
            }
        }

        private void OnSimWriteCompleted()
        {
            m_isWaitingForReply = false;
            WSValue op;
            lock (m_writeLock)
            {
                op = m_pendingWrites.FirstOrDefault();
                if (op == null)
                {
                    return;
                }
                m_pendingWrites.RemoveAt(0);
            }

            var data = new WriteToSim();

            if (op.value is double value)
            {
                data.isSet = 1;
                data.name = WriteToSim.AllocString(op.name.Remove(0, 2));
                data.value = value;
                data.index = m_localVarNames.IndexOf(op.name);
                // Trace.WriteLine($"LVAR: Set {op.name} to {op.value} (at {data.index})");
            }
            else if (op.value == null)
            {
                data.isSet = 0;
                data.name = WriteToSim.AllocString(op.name.Remove(0, 2));
                data.index = m_localVarNames.IndexOf(op.name);
                Trace.WriteLine($"LVAR: Register data {op.name} at {data.index}");
            }
            else
            {
                Trace.WriteLine("bad datatype");
            }

            data.lastCommandId = ++m_lastCommandId;

            m_isWaitingForReply = true;
            m_simConnect?.m_simConnect?.SetClientData(ClientData.WriteToSim,
                ClientData.WriteToSim, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, data);
            BridgeCounter.GotFrame();
        }

        private void MaybeTriggerWriteToSim()
        {
            if (IsConnected && !m_isWaitingForReply)
            {
                //   Trace.WriteLine("REstarting queueu");
                OnSimWriteCompleted();
            }
        }

        public void AdviseVariables(List<WSValue> nameList)
        {
            var lvars = nameList.Where(n => n.name.StartsWith("L:"));
            var cvars = nameList.Where(n => n.name.StartsWith("C:"));
            var evars = nameList.Where(n => n.name.StartsWith("E:"));
            var avars = nameList.Where(n => !n.name.StartsWith("L:") && !n.name.StartsWith("C:") && !n.name.StartsWith("E:"));

            foreach (var n in avars)
            {
                if (n.name.StartsWith("A:"))
                {
                    n.name = n.name.Remove(0, 2);
                }

                if (!m_aircraftVars.ContainsKey(n.name))
                {
                    if (IsConnected)
                    {
                        RequestSimData(n);
                    }
                    else
                    {
                        m_pendingAircraftVarRequests.Enqueue(n);
                    }
                }
                m_aircraftVars[n.name] = n;
            }

            foreach (var n in lvars)
            {
                if (!m_localVars.ContainsKey(n.name))
                {
                    m_localVarNames.Add(n.name);
                    m_localVars[n.name] = n;
                    lock (m_writeLock)
                    {
                        m_pendingWrites.Add(new WSValue { name = n.name, unit = n.unit });
                    }
                    MaybeTriggerWriteToSim();
                }
                else
                {
                    m_localVars[n.name] = n;
                }
            }
        }

        internal void Write(WSValue value)
        {
            value.name = value.name.ToUpper();
            if (value.name.StartsWith("K:"))
            {
                TriggerSimEvent(value.name, uint.Parse(value.value.ToString()));
            }
            else
            {
                if (value.name.StartsWith("C:"))
                {
                    return;
                }

                lock (m_writeLock)
                {
                    var existing = m_pendingWrites.FirstOrDefault(w => w.name == value.name && w.value != null);
                    if (existing != null)
                    {
                        //  Trace.WriteLine($"Update existing queue value {existing.name} {value.name} {value.value}");
                        existing.value = value.value;
                    }
                    else
                    {
                        if (!m_localVarNames.Contains(value.name))
                        {
                            m_localVarNames.Add(value.name);
                            m_localVars[value.name] = value;

                            //  Trace.WriteLine("Add GET: " + value.name);

                            m_pendingWrites.Add(new WSValue { name = value.name, unit = value.unit, value = value.value });

                            m_pendingWrites.Add(new WSValue { name = value.name, unit = value.unit });
                        }
                        else
                        {
                            m_pendingWrites.Add(new WSValue { name = value.name, unit = value.unit, value = value.value });

                        }
                        // Trace.WriteLine("Add SET: " + value.name);



                        MaybeTriggerWriteToSim();
                    }
                }
            }
        }

        private void RequestSimData(WSValue value)
        {
            var id = (m_lastDefineId++).ToEnum();
            var isString = value.unit.ToLower() == "string";

            m_simConnect.m_simConnect.AddToDataDefinition(id, value.name, !isString ? value.unit : null,
                isString ? SIMCONNECT_DATATYPE.STRING256 : SIMCONNECT_DATATYPE.FLOAT64,
                0.0f, SimConnect.SIMCONNECT_UNUSED);

            if (isString)
            {
                m_simConnect.m_simConnect.RegisterDataDefineStruct<SimDataString256>(id);
            }
            else
            {
                m_simConnect.m_simConnect.RegisterDataDefineStruct<SimDataDouble>(id);
            }

            Trace.WriteLine($"SIMCONNECT: Register data {value.name} ({value.unit}) {id}");

            var dataRequest = (m_lastDataRequestId++);
            m_simConnect.m_simConnect.RequestDataOnSimObject(dataRequest.ToEnum(), id, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            m_dataRequests[dataRequest] = value;
        }

        private void TriggerSimEvent(string name, uint value)
        {
            if (!IsConnected) return;

            name = name.Remove(0, 2); // K:
            if (!m_events.ContainsKey(name))
            {
                var nextEventId = (m_lastEvent++).ToEnum();
                m_events[name] = nextEventId;

                Trace.WriteLine($"SIMCONNECT: Mapping event {name} to {nextEventId}");
                m_simConnect.m_simConnect.MapClientEventToSimEvent(nextEventId, name);
            }
            Trace.WriteLine($"SIMCONNECT: Trigger event {name} ({value})");

            m_simConnect.m_simConnect.TransmitClientEvent(0U, m_events[name], value, (Enum)NOTIFICATION_GROUPS.GENERIC, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
    }
}
