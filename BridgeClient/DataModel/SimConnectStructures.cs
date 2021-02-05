using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BridgeClient.DataModel
{
    enum DATA_REQUESTS
    {
        ReadFromSimChanged,
        GenericData,
    }

    enum Structs
    {
        WriteToSim,
        ReadFromSim,
        GenericData,
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
        public double[] data;
        public int seq;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WriteToSim
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data4;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data5;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data6;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data7;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data8;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data9;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data10;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data11;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data12;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data13;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data14;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data15;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data16;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data17;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data18;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data19;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data20;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data21;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data22;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data23;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data24;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data25;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data26;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data27;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data28;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data29;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data30;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] data31;
        public int seq;

        public WriteToSim(string[] sData, int seqId)
        {
            seq = seqId;
            data1 = AllocString(sData[0]);
            data2 = AllocString(sData[1]);
            data3 = AllocString(sData[2]);
            data4 = AllocString(sData[3]);
            data5 = AllocString(sData[4]);
            data6 = AllocString(sData[5]);
            data7 = AllocString(sData[6]);
            data8 = AllocString(sData[7]);
            data9 = AllocString(sData[8]);
            data10 = AllocString(sData[9]);
            data11 = AllocString(sData[10]);
            data12 = AllocString(sData[11]);
            data13 = AllocString(sData[12]);
            data14 = AllocString(sData[13]);
            data15 = AllocString(sData[14]);
            data16 = AllocString(sData[15]);
            data17 = AllocString(sData[16]);
            data18 = AllocString(sData[17]);
            data19 = AllocString(sData[18]);
            data20 = AllocString(sData[19]);
            data21 = AllocString(sData[20]);
            data22 = AllocString(sData[21]);
            data23 = AllocString(sData[22]);
            data24 = AllocString(sData[23]);
            data25 = AllocString(sData[24]);
            data26 = AllocString(sData[25]);
            data27 = AllocString(sData[26]);
            data28 = AllocString(sData[27]);
            data29 = AllocString(sData[28]);
            data30 = AllocString(sData[29]);
            data31 = AllocString(sData[30]);
        }

        static byte[] AllocString(string txt)
        {
            return GetArrayOfSize(Encoding.ASCII.GetBytes(txt), 256);
        }

        static byte[] GetArrayOfSize(byte[] input, int size)
        {
            var ret = new byte[size];
            Array.Copy(input, ret, input.Length);
            return ret;
        }
    }

}
