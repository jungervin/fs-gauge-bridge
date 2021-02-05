using BridgeClient.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace BridgeClient.ViewModel
{
    class TextWithValue : BaseViewModel
    {
        public string Name { get => Get<string>(); set => Set(value); }
        public string Value { get => Get<string>(); set => Set(value); }
        public bool IsOK { get => Get<bool>(); set => Set(value); }

        public TextWithValue() => IsOK = true;
    }

    class GaugeInfo : BaseViewModel
    {
        public string Name { get => Get<string>(); set => Set(value); }
        public int Index { get => Get<int>(); set => Set(value); }
        public ICommand Command { get; set; }

        public GaugeInfo(string url)
        {
            Name = Path.GetFileName(url);
            Command = new RelayCommand(() =>
            {
                ((App)App.Current).Navigate(Index);
            });
        }
    }

    class MainWindowViewModel : BaseViewModel
    {
        public SimConnectViewModel SimConnect { get => Get<SimConnectViewModel>(); set => Set(value); }
        public List<GaugeInfo> Gauges { get => Get<List<GaugeInfo>>(); set => Set(value); }
        public List<TextWithValue> Data { get; }
        public string Title => "FS Gauge Bridge";
        public ICommand OpenLog { get; }
        public ICommand OpenVars { get; }

        public MainWindowViewModel(ICommand log, ICommand openVars)
        {
            OpenLog = log;
            OpenVars = openVars;

            var isSimConnected = new TextWithValue { Name = "SimConnect" };
            var simBridgeOps = new TextWithValue { Name = "Bridge ops/sec" };
            var bridgeQueueLength = new TextWithValue { Name = "Bridge Queue" };
            var bridgeLatency = new TextWithValue { Name = "Bridge Latency" };
            var trackedVAriables = new TextWithValue { Name = "Variable Count" };
            var acTitle = new TextWithValue { Name = "Aircraft Title" };
            var acLocation = new TextWithValue { Name = "Aircraft location" };

            Data = new List<TextWithValue> {
                isSimConnected,
                acTitle,
                simBridgeOps ,
               // bridgeQueueLength,
                bridgeLatency,
                trackedVAriables,
            };

            var savedTitle = SimConnect?.Title;

            var t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(0.2);
            t.Tick += (_, __) =>
            {
              //  SimConnect.ReadCounter.CalculateFps();

                isSimConnected.Value = SimConnect.IsConnected ? "Connected" : "Connecting...";
                isSimConnected.IsOK = SimConnect.IsConnected;

                simBridgeOps.Value = Math.Round(SimConnect.ReadCounter.Fps).ToString();
                simBridgeOps.IsOK = !SimConnect.IsConnected || SimConnect.ReadCounter.Fps > 1;

                trackedVAriables.Value = SimConnect.All.Count.ToString();
               
              //  bridgeQueueLength.Value = SimConnect.QueueLength().ToString();
              //  bridgeQueueLength.IsOK = SimConnect.QueueLength() < 200;
                bridgeLatency.Value = SimConnect.Latency + "ms";
                bridgeLatency.IsOK = SimConnect.Latency < 1000;

                acTitle.Value = SimConnect.Title;

                if (!string.IsNullOrWhiteSpace(SimConnect.Title))
                {
                    try
                    {
                        acLocation.Value = CfgManager.titleToAircraftDirectoryName[SimConnect.Title];
                    }
                    catch(Exception )
                    {
                        acLocation.Value = "Missing";
                    }
                }
                else
                {
                    acLocation.Value = "No aircraft loaded";
                }

                if (savedTitle != SimConnect.Title && SimConnect.Title != null)
                {
                    try
                    {
                        Gauges = LoadGauges();
                    }
                    catch (Exception) { }
                    savedTitle = SimConnect.Title;
                }
            };
            t.Start();
        }

        private List<GaugeInfo> LoadGauges()
        {
            var gauges = CfgManager.aircraftDirectoryNameToGaugeList[CfgManager.titleToAircraftDirectoryName[SimConnect.Title]];

            var gauges2 = gauges.Select(g => g.Split(',')[0].Trim()).Select(g => new GaugeInfo(g)).ToList();
            for(var i = 0; i < gauges2.Count; i++)
            {
                gauges2[i].Index = i;
            }
            return gauges2;
        }
    }
}
