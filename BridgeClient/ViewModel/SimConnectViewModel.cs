using BridgeClient.DataModel;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace BridgeClient
{
    enum DATA_REQUESTS
    {
        ReadFromSimChanged
    }

    enum Structs
    {
        WriteToSim,
        ReadFromSim,
    }

    enum ClientData
    {
        WriteToSim,
        ReadFromSim,
    }

    enum NOTIFICATION_GROUPS
    {
        GENERIC,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ReadFromSim
    {
        public double data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WriteToSim
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data;

        public WriteToSim(string strData)
        {
            var txtBytes = Encoding.ASCII.GetBytes(strData);
            var ret = new byte[256];
            Array.Copy(txtBytes, ret, txtBytes.Length);
            data = ret;
        }
    }

    class SimConnectViewModel : BaseViewModel
    {
        public event Action<double> DataChanged;

        public bool IsConnected { get => Get<bool>(); set => Set(value); }

        private SimConnector m_simConnect;

        public SimConnectViewModel()
        {
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                var t = new Thread(SimConnectThreadProc);
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            });
        }

        void SimConnectThreadProc()
        {
            m_simConnect = new SimConnector();
            m_simConnect.Connected += (isConnected) => IsConnected = isConnected;
            m_simConnect.Connect(() =>
            {
                m_simConnect.m_simConnect.MapClientDataNameToID("WriteToSim", ClientData.WriteToSim);
                m_simConnect.m_simConnect.MapClientDataNameToID("ReadFromSim", ClientData.ReadFromSim);

                var WriteToSimSize = (uint)Marshal.SizeOf(typeof(WriteToSim));
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.WriteToSim, 0, WriteToSimSize, 0, 0);

                var ReadFromSimSize = (uint)Marshal.SizeOf(typeof(ReadFromSim));
                m_simConnect.m_simConnect.AddToClientDataDefinition(ClientData.ReadFromSim, 0, ReadFromSimSize, 0, 0);

                // This enables OnRecvClientData to cast to the type.
                m_simConnect.m_simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ReadFromSim>(ClientData.ReadFromSim);

                m_simConnect.m_simConnect.OnRecvClientData += OnRecvClientData;
                m_simConnect.m_simConnect.RequestClientData(
                    ClientData.ReadFromSim,
                    DATA_REQUESTS.ReadFromSimChanged,
                    ClientData.ReadFromSim, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET, SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);
            });

            Dispatcher.Run();
        }

        private void OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            var simData = (ReadFromSim)(data.dwData[0]);
            Trace.WriteLine("Got changed data: " + simData.data);
            DataChanged?.Invoke(simData.data);
        }

        internal void SendCommand(string input)
        {
            Trace.WriteLine("Sending: " + input);
            m_simConnect?.m_simConnect?.SetClientData(
                ClientData.WriteToSim,
                ClientData.WriteToSim,
                SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT,
                0,
                new WriteToSim(input));
        }
    }
}
