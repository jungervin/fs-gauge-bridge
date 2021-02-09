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

    class ReadOperation
    {
        public string Key { get; set; }
        public string Unit { get; set; }
        public double? Value { get; set; }
    }

    class ReadOperationBatch
    {
        public List<ReadOperation> Lines { get; set; }
        public int SeqId { get; set; }

        public object getData()
        {
            List<string> ret = new List<string>();
            foreach (var vv in Lines)
            {
                var isValue = vv.Value.HasValue;
                var valueText = isValue ? Math.Round(vv.Value.Value, 4) + " " : "";
                var str = $"{valueText}({(isValue ? ">" : "")}{FixKey(vv.Key)},{vv.Unit})";

                if (isValue)
                {
                    //    Trace.WriteLine(str);
                }
                ret.Add(str);
            }

            return new WriteToSim(ret.ToArray(), SeqId);
        }

        private string FixKey(string key)
        {
            if (key[1] != ':')
            {
                key = "A:" + key;
            }
            return key;
        }
    }

    class SimConnectViewModel : BaseViewModel
    {
        public static SimConnectViewModel Instance;

        public bool IsConnected { get => Get<bool>(); set => Set(value); }
        public string Title { get => Get<string>(); set => Set(value); }
        public double Latency { get => Get<double>(); set => Set(value); }

        public FpsCounter BridgeCounter { get; }
        public FpsCounter SimConnectCounter { get; }

        private SimConnector m_simConnect;
        public Dictionary<string, object> All = new Dictionary<string, object>();
        public Dictionary<string, string> m_localVars = new Dictionary<string, string>();
        public Dictionary<string, WSValue> m_aircraftVars = new Dictionary<string, WSValue>();
        private List<string> m_localVarNames = new List<string>();
        private object m_allLock = new object();
        private object m_writeLock = new object();
        private ReadOperationBatch m_currentOperation;
        private int m_currentTypeIndex;
        private int m_seqId;
        private Queue<ReadOperation> m_pendingWrites = new Queue<ReadOperation>();
        private Queue<WSValue> m_pendingAircraftVarRequests = new Queue<WSValue>();

        private Stopwatch m_perCycleTimer = Stopwatch.StartNew();

        private Dictionary<uint, WSValue> m_dataRequests = new Dictionary<uint, WSValue>();

        private Dictionary<string, Enum> m_events = new Dictionary<string, Enum>();
        private uint m_lastEvent = 0;


        private uint m_lastDataRequestId;
        private uint m_lastDefineId;


        public SimConnectViewModel()
        {
            BridgeCounter = new FpsCounter();
            SimConnectCounter = new FpsCounter();
            m_seqId = new Random().Next(0, 500);
            Instance = this;
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                var t = new Thread(SimConnectThreadProc);
                t.IsBackground = true;
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            });


            m_localVars["LIGHT LANDING"] = "number";
            m_localVarNames.Add("LIGHT LANDING");

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



                m_simConnect.m_simConnect.MapClientDataNameToID("WriteToSim2", ClientData.WriteToSim);
                m_simConnect.m_simConnect.MapClientDataNameToID("ReadFromSim2", ClientData.ReadFromSim);
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.WriteToSim, 0, (uint)Marshal.SizeOf(typeof(WriteToSim)), 0, 0);
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.ReadFromSim, 0, (uint)Marshal.SizeOf(typeof(ReadFromSim)), 0, 0);

                m_simConnect.m_simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ReadFromSim>(ClientData.ReadFromSim);


                m_simConnect.m_simConnect.RequestClientData(
                    ClientData.ReadFromSim,
                    DATA_REQUESTS.ReadFromSimChanged,
                    ClientData.ReadFromSim, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);


                // Scenario: First loading the sim.
                // This is NOT necessary once BridgeGauge has set client data.
                RequestNextOperation();

                
                while (m_pendingAircraftVarRequests.Any())
                {
                    RequestSimData(m_pendingAircraftVarRequests.Dequeue());
                }
            });

            Dispatcher.Run();
        }

        private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var value = m_dataRequests[data.dwRequestID];

            if (data.dwData[0] is SimDataString256 simData)
            {
                value.value = simData.value;
                if (value.name.ToLower() == "title")
                {
                    Title = simData.value;
                }

            }
            else if (data.dwData[0] is SimDataDouble simDouble)
            {
                value.value = simDouble.value;

            }
            else
            {
                throw new NotImplementedException();
            }

            All[value.name] = value.value;

            SimpleHTTPServer.TakeOperation(value);
           // SimConnectCounter.GotFrame();

            /*
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.GenericData2:
                    OnGotSimConnectVariables((GENERIC_DATA)data.dwData[0]);
                    break;
                default:
                    throw new NotImplementedException($"{data.dwRequestID}");
            }
            */
        }



        public string GetJson()
        {
            lock (m_allLock)
            {
                return JsonConvert.SerializeObject(All);
            }
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
            var op = m_currentOperation;
            // Trace.WriteLine("OnRecvClientData " + (op == null ? "NONE" : op.SeqId.ToString()));
            if (op == null ||  // Initialize
                op.SeqId == data.seq) // Exact match
            {
                if (op != null)
                {
                    lock (m_allLock)
                    {
                        for (var i = 0; i < op.Lines.Count; i++)
                        {
                            All[op.Lines[i].Key] = data.data[i];
                            //  Trace.WriteLine($"{data.seq} {ro.Key[i]}: {data.data[i]}");
                        }
                    }
                    SimpleHTTPServer.TakeOperation(op, data.data);
                }

                BridgeCounter.GotFrame();
                RequestNextOperation();
            }
            else
            {
                if (op != null)
                {
                    if (op.SeqId == data.seq + 1)
                    {
                        // Ignore this is simply waiting still
                    }
                    else
                    {
                        Trace.WriteLine($"## OnRecvClientData Got {data.seq} was expecting {m_currentOperation.SeqId} ");
                    }
                }
                else
                {
                    // Scenario: Reconnecting with state already present.
                    Trace.WriteLine("Executing #1 request");
                    RequestNextOperation();
                }
            }
        }

        private void RequestNextOperation()
        {
            var nextOp = GetNextOperationBatch();
            m_currentOperation = nextOp;
            nextOp.SeqId = ++m_seqId;

            m_simConnect?.m_simConnect?.SetClientData(ClientData.WriteToSim, ClientData.WriteToSim, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                0, nextOp.getData());
        }

        ReadOperationBatch GetNextOperationBatch()
        {
            var op = new ReadOperationBatch();
            op.Lines = new int[31].Select(_ => GetNextOperation()).ToList();
            return op;
        }

        ReadOperation GetNextOperation()
        {
            // Writes are highest priority
            lock (m_writeLock)
            {
                if (m_pendingWrites.Count > 0)
                {
                    return m_pendingWrites.Dequeue();
                }
            }

            ++m_currentTypeIndex;
            // Loop through all known variables
            if (m_currentTypeIndex == 1)
            {
                Latency = m_perCycleTimer.ElapsedMilliseconds;
                m_perCycleTimer.Restart();
            }

            if (m_currentTypeIndex >= m_localVarNames.Count)
            {
                m_currentTypeIndex = 0;
            }

            var key = m_localVarNames[m_currentTypeIndex];
            return new ReadOperation { Key = key, Unit = m_localVars[key] };
        }


        public void AdviseVariables(List<WSValue> nameList)
        {
            var lvars = nameList.Where(n => n.name.StartsWith("L:"));
            var cvars = nameList.Where(n => n.name.StartsWith("C:"));
            var evars = nameList.Where(n => n.name.StartsWith("E:"));
            var avars = nameList.Where(n => !n.name.StartsWith("L:") && !n.name.StartsWith("C:") && !n.name.StartsWith("E:"));

            foreach (var n in avars)
            {
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
                }
                m_localVars[n.name] = n.unit;
            }
        }


        internal void Write(string name, string unit, double value)
        {
            name = name.ToUpper();
            // Trace.WriteLine($"Write: {name}={value} as {unit}");

            if (name.StartsWith("K:"))
            {
                TriggerSimEvent(name, (uint)value);
            }
            else
            {
                lock (m_writeLock)
                {
                    m_pendingWrites.Enqueue(new ReadOperation
                    {
                        Key = name,
                        Unit = unit,
                        Value = value
                    });
                }
            }
        }

        private void RequestSimData(WSValue value)
        {
         //   Trace.WriteLine("RequestSimData: " + value.name);

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

            var dataRequest = (m_lastDataRequestId++);
            m_simConnect.m_simConnect.RequestDataOnSimObject(dataRequest.ToEnum(), id, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            m_dataRequests[dataRequest] = value;
        }

        private void TriggerSimEvent(string name, uint value)
        {
            name = name.Remove(0, 2); // K:

          //  Trace.WriteLine("TRIGGER: " + name);

            if (!m_events.ContainsKey(name))
            {
                var nextEventId = (m_lastEvent++).ToEnum();
                m_events[name] = nextEventId;
                m_simConnect.m_simConnect.MapClientEventToSimEvent(nextEventId, name);
            }

            m_simConnect.m_simConnect.TransmitClientEvent(0U, m_events[name], value, (Enum)NOTIFICATION_GROUPS.GENERIC, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
    }
}
