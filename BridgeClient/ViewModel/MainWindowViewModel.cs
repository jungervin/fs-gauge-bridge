using System;
using System.Windows.Input;

namespace BridgeClient.ViewModel
{
    class MainWindowViewModel : BaseViewModel
    {
        public string Input { get => Get<string>(); set => Set(value); }
        public string Buffer { get => Get<string>(); set => Set(value); }
        public ICommand Send { get; }
        public SimConnectViewModel SimConnect { get; }

        public MainWindowViewModel(SimConnectViewModel simConnect)
        {
            SimConnect = simConnect;

            Send = new RelayCommand(() =>
            {
                simConnect.SendCommand(Input);
            });
            simConnect.DataChanged += (data) => Buffer = DateTime.Now.ToLongTimeString() + " " + data + "\n" + Buffer;

   
        }
    }
}
