using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace BridgeClient.DataModel
{
    public class SimConnector : NativeWindow
    {
        public event Action<bool> Connected;
        public event Action<SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE> SimConnectRecvSimobjectDataBytype;

        private static readonly int WM_USER_SIMCONNECT = 0x402;
        public SimConnect m_simConnect;
        private Action m_afterConnected;

        public SimConnector() : base()
        {

        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (m_simConnect != null)
                {
                    try
                    {
                        m_simConnect.ReceiveMessage();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Failed while talking to sim: " + ex);
                        doDisconnected();
                    }
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        public void Connect(Action afterConnected)
        {
            m_afterConnected = afterConnected;
            CreateHandle(new CreateParams());

            ConnectInternal();
        }

        private void ConnectInternal()
        {
            Trace.WriteLine("Connecting to sim...");
            try
            {
                m_simConnect = new SimConnect("BridgeGaugeClient", Handle, (uint)WM_USER_SIMCONNECT, null, 0);
                m_simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(OnSimConnectRecvOpen);
                m_simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(OnSimConnectRecvQuit);
                m_simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(OnSimConnectRecvException);
                
                m_afterConnected();
            }
            catch (COMException)
            {
                Trace.WriteLine("Failed to connect to sim");
                doDisconnected();
            }
        }

        private void OnSimConnectRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            Trace.WriteLine("Exception received: " + ((uint)data.dwException));
            doDisconnected();
        }

        private void OnSimConnectRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Trace.WriteLine("Connected to sim");
            Connected(true);
        }

        private void OnSimConnectRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Trace.WriteLine("Sim quit");
            doDisconnected();
        }


        private void doDisconnected()
        {
            if (m_simConnect != null)
            {
                m_simConnect.Dispose();
                m_simConnect = null;
            }
            Connected(false);

            ReconnectAsync();
        }

        private async void ReconnectAsync()
        {
            await Task.Delay(10 * 1000);
            ConnectInternal();
        }
    }
}