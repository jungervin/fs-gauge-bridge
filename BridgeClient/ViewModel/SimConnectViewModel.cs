using BridgeClient.DataModel;
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
                ret.Add($"{valueText}({(isValue ? ">" : "")}{FixKey(vv.Key)},{vv.Unit})");
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

        public FpsCounter ReadCounter { get; }

        private SimConnector m_simConnect;
        public Dictionary<string, object> All = new Dictionary<string, object>();
        public Dictionary<string, string> m_types = new Dictionary<string, string>();
        private List<string> m_typesRaw = new List<string>();
        private object m_allLock = new object();
        private object m_writeLock = new object();
        private ReadOperationBatch m_currentOperation;
        private int m_currentTypeIndex;
        private int m_seqId;
        private readonly DispatcherTimer m_refreshTimer = new DispatcherTimer();

        private List<string> m_StringAVarsToCopyFromSimData = new List<string>();
        private Queue<ReadOperation> m_pendingWrites = new Queue<ReadOperation>();

        private Stopwatch m_perCycleTimer = Stopwatch.StartNew();

        public SimConnectViewModel()
        {
            ReadCounter = new FpsCounter();
            m_seqId = new Random().Next(0, 500);
            Instance = this;
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                var t = new Thread(SimConnectThreadProc);
                t.IsBackground = true;
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            });

            m_StringAVarsToCopyFromSimData = new List<string>
            {
                "ATC MODEL",
                "TITLE",
                "HSI STATION IDENT",
                "GPS WP PREV ID",
                "GPS WP NEXT ID",
                "NAV IDENT:1",
                "NAV IDENT:2",
                "NAV IDENT:3",
                "NAV IDENT:4",
            };

            m_types["ABSOLUTE TIME"] = "seconds";

            m_refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
            m_refreshTimer.Tick += RefreshTimer_Tick;
            m_refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            m_simConnect?.m_simConnect?.RequestDataOnSimObjectType(DATA_REQUESTS.GenericData, Structs.GenericData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
        }

        void SimConnectThreadProc()
        {
            m_simConnect = new SimConnector();
            m_simConnect.Connected += (isConnected) => IsConnected = isConnected;
            m_simConnect.Connect(() =>
            {
                m_simConnect.RegisterStruct<GENERIC_DATA>(Structs.GenericData);

                m_simConnect.m_simConnect.MapClientDataNameToID("WriteToSim2", ClientData.WriteToSim);
                m_simConnect.m_simConnect.MapClientDataNameToID("ReadFromSim2", ClientData.ReadFromSim);

                var WriteToSimSize = (uint)Marshal.SizeOf(typeof(WriteToSim));
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.WriteToSim, 0, WriteToSimSize, 0, 0);

                var ReadFromSimSize = (uint)Marshal.SizeOf(typeof(ReadFromSim));
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.ReadFromSim, 0, ReadFromSimSize, 0, 0);

                m_simConnect.m_simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ReadFromSim>(ClientData.ReadFromSim);

                m_simConnect.m_simConnect.OnRecvClientData += OnRecvClientData;
                m_simConnect.m_simConnect.OnRecvSimobjectDataBytype += OnRecvSimobjectDataBytype;

                m_simConnect.m_simConnect.RequestClientData(
                    ClientData.ReadFromSim,
                    DATA_REQUESTS.ReadFromSimChanged,
                    ClientData.ReadFromSim, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);

            });

            Dispatcher.Run();
        }

        private void OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.GenericData:
                    OnGotSimConnectVariables((GENERIC_DATA)data.dwData[0]);
                    break;
                default:
                    throw new NotImplementedException($"{data.dwRequestID}");
            }
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

        private void OnGotSimConnectVariables(GENERIC_DATA data)
        {
            lock (m_allLock)
            {
                All["TITLE"] = data.title;
                All["HSI STATION IDENT"] = data.hsi_station_ident;
                All["GPS WP NEXT ID"] = data.gps_wp_next_id;
                All["GPS WP PREV ID"] = data.gps_wp_prev_id;
                All["ATC MODEL"] = data.atc_model;
                All["NAV IDENT:1"] = data.nav_ident_1;
                All["NAV IDENT:2"] = data.nav_ident_2;
                All["NAV IDENT:3"] = data.nav_ident_3;
                All["NAV IDENT:4"] = data.nav_ident_4;
            }
            Title = data.title;
        }

        private void OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA recvData)
        {
            var data = (ReadFromSim)(recvData.dwData[0]);
            var op = m_currentOperation;
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
                }

                ReadCounter.GotFrame();

                var nextOp = GetNextOperationBatch();
                m_currentOperation = nextOp;
                nextOp.SeqId = ++m_seqId;

                m_simConnect?.m_simConnect?.SetClientData(
                    ClientData.WriteToSim,
                    ClientData.WriteToSim,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                    0,
                    nextOp.getData());
            }
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

            //  ++m_currentTypeIndex;
            ++m_currentTypeIndex;
            // Loop through all known variables
            if (m_currentTypeIndex == 1)
            {
                Latency = m_perCycleTimer.ElapsedMilliseconds;
                m_perCycleTimer.Restart();
            }

            while (m_currentTypeIndex < m_typesRaw.Count - 1 && m_StringAVarsToCopyFromSimData.Contains(m_typesRaw[m_currentTypeIndex]))
            {
                ++m_currentTypeIndex;

            }

            if (m_currentTypeIndex >= m_typesRaw.Count)
            {
                m_currentTypeIndex = 0;
            }

            var key = m_typesRaw[m_currentTypeIndex];
            return new ReadOperation { Key = key, Unit = m_types[key] };
        }

        public void AdviseVariables(List<GetSimVarValueData> nameList)
        {
            nameList = nameList.Where(x => !m_StringAVarsToCopyFromSimData.Contains(x.name)).ToList();

            foreach (var kv in nameList)
            {
                if (kv.unit.ToLower() == "string")
                {
                    Trace.WriteLine("### String that should be copied from SimData: " + kv.name);
                }
            }

            foreach (var n in nameList)
            {
                if (!m_types.ContainsKey(n.name))
                {
                    m_typesRaw.Add(n.name);

                }
                m_types[n.name] = n.unit;
            }
        }

        internal void Write(string name, string unit, double value)
        {
            var op = new ReadOperation
            {
                Key = name,
                Unit = unit,
                Value = value
            };
            lock (m_writeLock)
            {
                m_pendingWrites.Enqueue(op);
            }
        }
    }
}
