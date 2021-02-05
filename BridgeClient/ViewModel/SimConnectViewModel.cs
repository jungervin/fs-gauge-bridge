using BridgeClient.DataModel;
using Microsoft.FlightSimulator.SimConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace BridgeClient
{

    class SimConnectViewModel : BaseViewModel
    {
        class Operation
        {
            public int SeqId { get; set; }
            public virtual object getData() => null;
            public Action Complete { get; set; }
        }

        class ReadOperation : Operation
        {
            public string[] Key { get; set; }
            public string[] Unit { get; set; }

            public ReadOperation()
            {
                Key = new string[31];
                Unit = new string[31];
            }

            public override object getData()
            {
                return new WriteToSim(new string[] {
                    $"({FixKey(Key[0])},{Unit[0]})",
                    $"({FixKey(Key[1])},{Unit[1]})",
                    $"({FixKey(Key[2])},{Unit[2]})",
                    $"({FixKey(Key[3])},{Unit[3]})",
                    $"({FixKey(Key[4])},{Unit[4]})",
                    $"({FixKey(Key[5])},{Unit[5]})",
                    $"({FixKey(Key[6])},{Unit[6]})",
                    $"({FixKey(Key[7])},{Unit[7]})",
                    $"({FixKey(Key[8])},{Unit[8]})",
                    $"({FixKey(Key[9])},{Unit[9]})",
                    $"({FixKey(Key[10])},{Unit[10]})",
                    $"({FixKey(Key[11])},{Unit[11]})",
                    $"({FixKey(Key[12])},{Unit[12]})",
                    $"({FixKey(Key[13])},{Unit[13]})",
                    $"({FixKey(Key[14])},{Unit[14]})",
                    $"({FixKey(Key[15])},{Unit[15]})" ,
                    $"({FixKey(Key[16])},{Unit[16]})" ,
                    $"({FixKey(Key[17])},{Unit[17]})" ,
                    $"({FixKey(Key[18])},{Unit[18]})" ,
                    $"({FixKey(Key[19])},{Unit[19]})" ,
                    $"({FixKey(Key[20])},{Unit[20]})" ,
                    $"({FixKey(Key[21])},{Unit[21]})" ,
                    $"({FixKey(Key[22])},{Unit[22]})" ,
                    $"({FixKey(Key[23])},{Unit[23]})" ,
                    $"({FixKey(Key[24])},{Unit[24]})" ,
                    $"({FixKey(Key[25])},{Unit[25]})" ,
                    $"({FixKey(Key[26])},{Unit[26]})" ,
                    $"({FixKey(Key[27])},{Unit[27]})" ,
                    $"({FixKey(Key[28])},{Unit[28]})" ,
                    $"({FixKey(Key[29])},{Unit[29]})" ,
                    $"({FixKey(Key[30])},{Unit[30]})"

                },
                    SeqId);
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

        public KeyValuePair<string,object>[] GetAllSafe()
        {
            lock (m_allLock)
            {
                return All.ToArray();
            }
        }

        class WriteOperation : Operation
        {
            public string Key { get; set; }
            public string Unit { get; set; }
            public double Value { get; set; }
            public override object getData()
            {
                var key = Key;
                var isKey = key[1] == ':' && key[0] == 'K';



                if (key[1] != ':')
                {
                    key = "A:" + key;
                }
                var SET = $"{Math.Round(Value, 3)} (>{key},{Unit})";
                //  Trace.WriteLine("SET: " + SET);

                if (isKey)
                {
                    //  Trace.WriteLine("#### KEY " + SET);
                }

                return new WriteToSim(new string[]{ SET,
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)",
                    "(A:LIGHT LANDING, Bool)" },
                    SeqId);
            }
        }

        internal int QueueLength()
        {
            return m_operations.Count;
        }

        public static SimConnectViewModel Instance;


        public bool IsConnected { get => Get<bool>(); set => Set(value); }
        public string Title { get => Get<string>(); set => Set(value); }
        public double Latency { get => Get<double>(); set => Set(value); }

        public FpsCounter ReadCounter { get; }

        private SimConnector m_simConnect;
        private int m_seqId;
        private Dictionary<int, string> m_requests = new Dictionary<int, string>();
        public Dictionary<string, object> All = new Dictionary<string, object>();
        public Dictionary<string, string> m_types = new Dictionary<string, string>();
        private Dispatcher m_simConnectDispatcher = null;
        private object m_operationLock = new object();
        private object m_allLock = new object();
        private Queue<Operation> m_operations = new Queue<Operation>();
        private Operation m_currentOperation;
        private readonly DispatcherTimer m_refreshTimer = new DispatcherTimer();

        private List<string> m_StringAVarsToCopyFromSimData = new List<string>();

        private bool m_isReadingOperationInProgress = false;
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
                "ATC MODEL", "TITLE", "HSI STATION IDENT", "GPS WP NEXT ID",
            };

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
            m_simConnectDispatcher = Dispatcher.CurrentDispatcher;
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

                // This enables OnRecvClientData to cast to the type.
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
                    Data((GENERIC_DATA)data.dwData[0]);
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

        private void Data(GENERIC_DATA data)
        {
            lock (m_allLock)
            {
                All["TITLE"] = data.title;
                All["HSI STATION IDENT"] = data.hsi_station_ident;
                All["GPS WP NEXT ID"] = data.gps_wp_next_id;
                All["ATC MODEL"] = data.atc_model;
            }
            Title = data.title;
        }

        private void OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA recvData)
        {
            // Trace.WriteLine("OnRecvClientData: " + simData.seq);

            var data = (ReadFromSim)(recvData.dwData[0]);

            if (m_currentOperation != null && m_currentOperation.SeqId == data.seq)
            {
                if (m_currentOperation is WriteOperation wo)
                {
                    lock (m_allLock)
                    {
                        All[wo.Key] = Math.Round(data.data[0], 4);
                    }
                    ReadCounter.GotFrame();
                    //   Trace.WriteLine($"WRITE {data.seq} {wo.Key}: {data.data1}");

                }
                else if (m_currentOperation is ReadOperation ro)
                {
                    lock (m_allLock)
                    {
                        for (var i = 0; i < ro.Key.Length; i++)
                        {
                            All[ro.Key[i]] = data.data[i];
                            //  Trace.WriteLine($"{data.seq} {ro.Key[i]}: {data.data[i]}");
                        }

                    }
                    ReadCounter.GotFrame();

                    //   Trace.WriteLine($"{data.seq} {ro.Key1}: {data.data1}");
                    //   Trace.WriteLine($"{data.seq} {ro.Key2}: {data.data2}");
                    //   Trace.WriteLine($"{data.seq} {ro.Key3}: {data.data3}");
                    //  Trace.WriteLine($"{data.seq} {ro.Key4}: {data.data4}");
                }

                var op = m_currentOperation;



                ProcessNextCommand();

                if (op.Complete != null)
                {
                    op.Complete();
                }
            }
            else
            {
                //  Trace.WriteLine($"{data.seq} ------------ expected: {m_currentOperation?.SeqId ?? -999}");

                if (m_currentOperation == null)
                {
                    ProcessNextCommand();
                }
            }
        }

        void ProcessNextCommand()
        {
           // Operation op = null;
            lock (m_operationLock)
            {

                m_currentOperation = null;

               // if (m_operations.Count > 0)
               // {
              //     op = m_operations.Dequeue();
               // }
            }

           // bool needsToStart = false;
           // lock (m_operationLock)
            {
          //      needsToStart = (m_operations.Count > 0);
            }

           // if (needsToStart)
            {
                ProcessSingleOperation();
            }
        }

        internal void Read(List<GetSimVarValueData> nameList)
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
                m_types[n.name] = n.unit;
            }

            if (!m_isReadingOperationInProgress && m_types.Count > 0)
            {
                TriggerReadAllVariables();
                m_isReadingOperationInProgress = true;
            }
        }

        private void TriggerReadAllVariables()
        {
            m_perCycleTimer = Stopwatch.StartNew();

            List<ReadOperation> ops = new List<ReadOperation>();
            foreach (var group in SplitList(m_types.Keys.ToList(), 31))
            {
                var ro = new ReadOperation();

                string k = null;
                string u = null;
                for (var i = 0; i < 31; i++)
                {
                    k = group.Count > i ? group[i] : k;
                    u = group.Count > i ? m_types[group[i]] : u;

                    ro.Key[i] = k;
                    ro.Unit[i] = u;
                }
                ops.Add(ro);
            }

            var lastop = ops.LastOrDefault();

            if (lastop != null)
            {
                lastop.Complete = () =>
                {
                    Latency = m_perCycleTimer.ElapsedMilliseconds;

                    TriggerReadAllVariables();
                };
            }


            foreach (var rox in ops)
            {
                QueueOperation(rox);
            }
        }

        static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        internal void Write(string input, string type, double value)
        {
            QueueOperation(new WriteOperation { Key = input, Unit = type, Value = value });
        }

        void QueueOperation(Operation op)
        {
           // if (!IsConnected) return;

            bool needsToStart = false;
            lock (m_operationLock)
            {
                m_operations.Enqueue(op);
                needsToStart = (m_operations.Count == 1) && m_currentOperation == null;
            }

            if (needsToStart)
            {
                ProcessSingleOperation();
            }
        }

        void ProcessSingleOperation()
        {
            m_simConnectDispatcher.InvokeAsync(() =>
            {
                Operation op = null;
                lock (m_operationLock)
                {
                    if (m_operations.Count > 0)
                    {
                        op = m_operations.Dequeue();
                        m_currentOperation = op;

                        op.SeqId = ++m_seqId;
                    }
                }

                if (op != null)
                {
                    m_simConnect?.m_simConnect?.SetClientData(
                        ClientData.WriteToSim,
                        ClientData.WriteToSim,
                        SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                        0,
                        op.getData());
                }
            });
        }
    }
}
