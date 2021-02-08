using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace BridgeClient.DataModel
{
    public class UITraceListener : TraceListener
    {
        public event Action<string> Message;

        private Dispatcher m_dispatcher = Dispatcher.CurrentDispatcher;

        public UITraceListener()
        {

        }

        void WriteImpl(string message)
        {
            Message?.Invoke(message);
        }

        public override void Write(string message)
        {
            if (m_dispatcher.Thread == Thread.CurrentThread)
            {
                WriteImpl(message);
            }
            else
            {
                m_dispatcher.InvokeAsync(() => WriteImpl(message));
            }

        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}
