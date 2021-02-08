using System;

namespace BridgeClient.ViewModel
{
    class LogWindowViewModel : BaseViewModel
    {
        public string Buffer { get => Get<string>(); set => Set(value); }
        public string Title => "Log";

        public void AddMessage(string message)
        {
            Buffer = Buffer + DateTime.Now.ToLongTimeString() + $" {message}" ;
        }
    }
}
