using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeClient.ViewModel
{
    class LogWindowViewModel : BaseViewModel
    {
        public string Buffer { get => Get<string>(); set => Set(value); }
        public string Title => "Log";

        public void AddMessage(string message)
        {
            Buffer = DateTime.Now.ToLongTimeString() + $" {message}" + Buffer;
        }
    }
}
