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

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class SimLinkAttribute : Attribute
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GENERIC_DATA
    {
        [SimLink(Name = "Title", Type = null)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string title;
        [SimLink(Name = "HSI STATION IDENT", Type = null)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string hsi_station_ident;
        [SimLink(Name = "GPS WP NEXT ID", Type = null)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string gps_wp_next_id;
        [SimLink(Name = "ATC MODEL", Type = null)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string atc_model;

    }

    public class SimConnector : NativeWindow
    {
        public event Action<bool> Connected;

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

        public void RegisterStruct<TData>(Enum definition)
        {
            foreach (var field in typeof(TData).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = field.GetCustomAttributes<SimLinkAttribute>().First();
                m_simConnect.AddToDataDefinition(definition, attr.Name, attr.Type, attr.Type == null ? SIMCONNECT_DATATYPE.STRING256 : SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            }
            m_simConnect.RegisterDataDefineStruct<TData>(definition);

        }
    }
}