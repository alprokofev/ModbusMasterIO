using System;
using System.Collections.Generic;
using SystemTable.SystemTools;
using PlatformExtensions;
using RuntimeTable;

namespace ModbusMasterIO
{
    internal class MbSignalState : IComparable<MbSignalState>
    {
        public event StateHandler OnMessage;
        public string SignalName { get; private set; }
        public uint FunctionNumber { get; private set; }
        public ushort Register { get; private set; }
        public string DataType { get; private set; }
        public string ByteOrder { get; private set; }
        public ushort Count { get; set; }
        public bool Invert { get; private set; }
        public string SignalType { get; private set; }
        public bool RawValueBandExist { get; private set; }
        public bool EuValueBandExist { get; private set; }
        public double MinEu { get; private set; }
        public double MaxEu { get; private set; }
        public double MinRaw { get; private set; }
        public double MaxRaw { get; private set; }
        private readonly LiveState liveState = new LiveState();

        public MbSignalState GetInstance(KeyValuePair<string, SignalInfo> signalInfo)
        {
            SignalName = signalInfo.Key;
            string sourceConnectionString = signalInfo.Value.SourceConnectionString;
            if (string.IsNullOrEmpty(sourceConnectionString))
                return null;

            DataType = "ushort";
            ByteOrder = "";
            Count = 1;
            string[] arrSource = sourceConnectionString.Split(',');
            foreach (var source in arrSource)
            {
                string localSource = source.Trim();
                string[] arrMbParam = localSource.Split(':');

                if (arrMbParam.Length != 2)
                    return null;

                if (string.Equals(arrMbParam[0], "mbFunctionNumber"))
                    FunctionNumber = Convert.ToByte(arrMbParam[1].Trim());

                if (string.Equals(arrMbParam[0], "reg"))
                    Register = Convert.ToUInt16(arrMbParam[1].Trim());

                if (string.Equals(arrMbParam[0], "type"))
                    DataType = arrMbParam[1].Trim();

                if (string.Equals(arrMbParam[0], "byteOrder"))
                    ByteOrder = arrMbParam[1].Trim();

                if (string.Equals(arrMbParam[0], "count"))
                    Count = ushort.Parse(arrMbParam[1].Trim());
            }

            if (signalInfo.Value.ScaleList != null && signalInfo.Value.ScaleList.Count != 0)
            {
                foreach (var scale in signalInfo.Value.ScaleList)
                {
                    if (string.Equals(scale.ScaleName, "euValueBand"))
                    {
                        EuValueBandExist = true;
                        MinEu = scale.MinScaleValue;
                        MaxEu = scale.MaxScaleValue;
                    }
                    if (string.Equals(scale.ScaleName, "rawValueBand"))
                    {
                        RawValueBandExist = true;
                        MinRaw = scale.MinScaleValue;
                        MaxRaw = scale.MaxScaleValue;
                    }
                }
            }
            SignalType = signalInfo.Value.MainAttributes.SignalType;
            Invert = signalInfo.Value.MainAttributes.Invert;
            return this;
        }

        public void SetValue(double value)
        {
            if (!liveState[SignalName].ChannelLink)
            {
                AddMessage("channel switchedOff", DebugLevel.FullModeLevel);
                return;
            }

            if (string.Equals(SignalType, "discrete"))
            {
                if (Invert)
                {
                    value = 1 + value * (-1);
                }
            }
            else
            {
                if (RawValueBandExist && (value < MinRaw || value > MaxRaw))
                {
                    AddMessage("address = " + Register + " exceeded the permissible limits. Expect [" +
                        MinRaw + "; " + MaxRaw + "], get " + value, DebugLevel.FullModeLevel);
                    return;
                }

                if (RawValueBandExist && EuValueBandExist)
                {
                    value = ((value - MinRaw)/(MaxRaw - MinRaw))*(MaxEu - MinEu) + MinEu;
                }

                if (string.Equals(DataType, "ushort") || (string.Equals(DataType, "short")))
                {
                    //преобразования нет
                }
                else if (string.Equals(DataType, "word"))
                {
                    if (string.Equals(ByteOrder, "1-0"))
                    {
                        ushort word = Convert.ToUInt16(value);
                        byte b1 = Convert.ToByte(word / 0x100);
                        byte b2 = Convert.ToByte(word % 0x100);
                        value = b1 + (b2*256);
                    }
                }
            }
            liveState[SignalName].Value = Math.Round(value, 2);
            AddMessage("address = " + Register + " value = " + Math.Round(value, 2), DebugLevel.FullModeLevel);
        }

        public void SetValue(double value1, double value2)
        {
            if (!liveState[SignalName].ChannelLink)
            {
                AddMessage("channel switchedOff", DebugLevel.FullModeLevel);
                return;
            }

            ushort word1 = Convert.ToUInt16(value1);
            ushort word2 = Convert.ToUInt16(value2);
            double value = 0.0;
            byte b1 = 0; byte b2 = 0; byte b3 = 0; byte b4 = 0;
            if (string.Equals(ByteOrder, "") || string.Equals(ByteOrder, "0-1-2-3"))
            {
                b1 = Convert.ToByte(word1 / 0x100);
                b2 = Convert.ToByte(word1 % 0x100);
                b3 = Convert.ToByte(word2 / 0x100);
                b4 = Convert.ToByte(word2 % 0x100);
            }
            if (string.Equals(ByteOrder, "1-0-3-2"))
            {
                b1 = Convert.ToByte(word1 % 0x100);
                b2 = Convert.ToByte(word1 / 0x100);
                b3 = Convert.ToByte(word2 % 0x100);
                b4 = Convert.ToByte(word2 / 0x100);
            }
            if (string.Equals(ByteOrder, "3-2-1-0"))
            {
                b1 = Convert.ToByte(word2 % 0x100);
                b2 = Convert.ToByte(word2 / 0x100);
                b3 = Convert.ToByte(word1 % 0x100);
                b4 = Convert.ToByte(word1 / 0x100);
            }
            if (string.Equals(ByteOrder, "2-3-0-1"))
            {
                b1 = Convert.ToByte(word2 / 0x100);
                b2 = Convert.ToByte(word2 % 0x100);
                b3 = Convert.ToByte(word1 / 0x100);
                b4 = Convert.ToByte(word1 % 0x100);
            }
            if (string.Equals(DataType, "dword"))
            {
                value = b1 + (b2*256) + (b3*256*256) + (b4*256*256*256);
            }
            else if (string.Equals(DataType, "float"))
            {
                var dim = new byte[] {b1, b2, b3, b4};
                value = BitConverter.ToSingle(dim, 0);
                value = Math.Round(value, 2);
                liveState[SignalName].Value = value;
            }

            if (EuValueBandExist && (value < MinEu || value > MaxEu))
            {
                AddMessage("address = " + Register + " exceeded the permissible limits. Expect [" +
                    MinEu + "; " + MaxEu + "], get " + value, DebugLevel.FullModeLevel);
                return;
            }
            liveState[SignalName].Value = Math.Round(value, 2);
            AddMessage("address = " + Register + " value = " + Math.Round(value, 2), DebugLevel.FullModeLevel);
        }

        public void SetValue(double value1, double value2, double value3, double value4)
        {
            if (!liveState[SignalName].ChannelLink)
            {
                AddMessage("channel switchedOff", DebugLevel.FullModeLevel);
                return;
            }

            ushort word1 = Convert.ToUInt16(value1);
            ushort word2 = Convert.ToUInt16(value2);
            ushort word3 = Convert.ToUInt16(value3);
            ushort word4 = Convert.ToUInt16(value4);

            var arrByte = new byte[8];
            if (string.Equals(ByteOrder, "") || string.Equals(ByteOrder, "0-1-2-3-4-5-6-7"))
            {
                arrByte[0] = Convert.ToByte(word1 / 0x100);
                arrByte[1] = Convert.ToByte(word1 % 0x100);
                arrByte[2] = Convert.ToByte(word2 / 0x100);
                arrByte[3] = Convert.ToByte(word2 % 0x100);
                arrByte[4] = Convert.ToByte(word3 / 0x100);
                arrByte[5] = Convert.ToByte(word3 % 0x100);
                arrByte[6] = Convert.ToByte(word4 / 0x100);
                arrByte[7] = Convert.ToByte(word4 % 0x100);
            }
            if (string.Equals(ByteOrder, "1-0-3-2-5-4-7-6"))
            {
                arrByte[0] = Convert.ToByte(word1 % 0x100);
                arrByte[1] = Convert.ToByte(word1 / 0x100);
                arrByte[2] = Convert.ToByte(word2 % 0x100);
                arrByte[3] = Convert.ToByte(word2 / 0x100);
                arrByte[4] = Convert.ToByte(word3 % 0x100);
                arrByte[5] = Convert.ToByte(word3 / 0x100);
                arrByte[6] = Convert.ToByte(word4 % 0x100);
                arrByte[7] = Convert.ToByte(word4 / 0x100);
            }
            if (string.Equals(ByteOrder, "7-6-5-4-3-2-1-0"))
            {
                arrByte[0] = Convert.ToByte(word4 % 0x100);
                arrByte[1] = Convert.ToByte(word4 / 0x100);
                arrByte[2] = Convert.ToByte(word3 % 0x100);
                arrByte[3] = Convert.ToByte(word3 / 0x100);
                arrByte[4] = Convert.ToByte(word2 % 0x100);
                arrByte[5] = Convert.ToByte(word2 / 0x100);
                arrByte[6] = Convert.ToByte(word1 % 0x100);
                arrByte[7] = Convert.ToByte(word1 / 0x100);
            }
            if (string.Equals(ByteOrder, "6-7-4-5-2-3-0-1"))
            {
                arrByte[0] = Convert.ToByte(word4 / 0x100);
                arrByte[1] = Convert.ToByte(word4 % 0x100);
                arrByte[2] = Convert.ToByte(word3 / 0x100);
                arrByte[3] = Convert.ToByte(word3 % 0x100);
                arrByte[4] = Convert.ToByte(word2 / 0x100);
                arrByte[5] = Convert.ToByte(word2 % 0x100);
                arrByte[6] = Convert.ToByte(word1 / 0x100);
                arrByte[7] = Convert.ToByte(word1 % 0x100);
            }
            double value = BitConverter.ToDouble(arrByte, 0);
            value = Math.Round(value, 2);
            if (EuValueBandExist && (value < MinEu || value > MaxEu))
            {
                AddMessage("address" + Register + "exceeded the permissible limits. Expect [" +
                    MinEu + "; " + MaxEu + "], get " + value, DebugLevel.FullModeLevel);
                return;
            }
            liveState[SignalName].Value = Math.Round(value, 2);
            AddMessage("address = " + Register + " value = " + Math.Round(value, 2), DebugLevel.FullModeLevel);
        }

        private void AddMessage(string messageObject, int currentLevel)
        {
            var e = new MessageEventArgs(SignalName + " " + messageObject, currentLevel);
            StateHandler handler = OnMessage;
            if (handler != null)
                handler(this, e);
        }

        public int CompareTo(MbSignalState other)
        {
            return Register.CompareTo(other.Register);
        }
    }
}