using System;
using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using ModbusMasterIO.Maps;
using PlatformExtensions;
using PlcEnvironment;
using RuntimeTable;

namespace ModbusMasterIO.Functions
{
    internal class WriteMultipleRegisters : WriteMap
    {
        public WriteMultipleRegisters(byte id, IModbusCommand mbCommand, DeviceInfo device, object locker)
            : base(id, mbCommand, device, locker)
        {
            modbusMapType = ModbusMapType.WriteMultipleRegisters;
        }

        protected override void WriteFunction()
        {
            try
            {
                var signalState = (SignalState) PlcThread.GetState();
                var signalName = signalState.SignalName;
                var mbInfo = dicSignals[signalName];
                var register = mbInfo.Register;
                var minEu = mbInfo.MinEu;
                var maxEu = mbInfo.MaxEu;
                var minRaw = mbInfo.MinRaw;
                var maxRaw = mbInfo.MaxRaw;
                var euValueBand = mbInfo.EuValueBandExist;
                var rawValueBand = mbInfo.RawValueBandExist;
                var type = mbInfo.DataType;
                var byteOrder = mbInfo.ByteOrder;
                var value = signalState.Value;
                var data = new ushort[] {0, 0};

                //записываемое значение должно быть в инженерных пределах
                if (euValueBand && (value < minEu || value > maxEu))
                {
                    AddMessage(signalName + ": exceeded the permissible limits. Expect [" + minEu + "; " +
                        maxEu + "], try to write value [" + value + "]", DebugLevel.FullModeLevel);
                    return;
                }

                //если заданы пределы масштабирования - преобразуем
                if (euValueBand && rawValueBand)
                    value = (value - minEu)/(maxEu - minEu)*(maxRaw - minRaw) + minRaw;

                if (string.IsNullOrEmpty(type))
                    type = "word";

                double rawValue = value;
                switch (type)
                {
                    case "ushort":
                        {
                            var temp = (ushort)value;
                            if (byteOrder.Equals("") || byteOrder.Equals("0-1"))
                                data[1] = temp;

                            if (byteOrder.Equals("1-0"))
                                data[0] = temp;
                            break;
                        }
                    case "word":
                        {
                            //если надо поменять байты местами
                            if (byteOrder.Equals("1-0"))
                            {
                                var word = (uint) value;
                                byte b0 = Convert.ToByte(word/0x100);
                                byte b1 = Convert.ToByte(word%0x100);
                                value = 256*b1 + b0;
                                AddMessage("Invert byte order [1-0]. Raw value: " + rawValue + ". New value: " + value,
                                    DebugLevel.FullModeLevel);
                            }
                            data[0] = (ushort)value;
                            break;
                        }
                    case "float":
                        {
                            float fValue = Convert.ToSingle(value);
                            byte[] bytes = BitConverter.GetBytes(fValue);
                            if (bytes.Length == 4)
                            {
                                if (byteOrder.Equals("") || byteOrder.Equals("0-1-2-3"))
                                {
                                    var word1 = (ushort)(bytes[0]*256 + bytes[1]);
                                    var word2 = (ushort)(bytes[2]*256 + bytes[3]);
                                    data = new[] {word1, word2};
                                }
                                else if (byteOrder.Equals("1-0-3-2"))
                                {
                                    var word1 = (ushort)(bytes[1] * 256 + bytes[0]);
                                    var word2 = (ushort)(bytes[3] * 256 + bytes[2]);
                                    data = new[] {word1, word2};
                                }
                                else if (byteOrder.Equals("3-2-1-0"))
                                {
                                    var word1 = (ushort)(bytes[0] * 256 + bytes[1]);
                                    var word2 = (ushort)(bytes[2] * 256 + bytes[3]);
                                    data = new[] {word2, word1};
                                }
                                //работает на хлораторной НФС5
                                else if (byteOrder.Equals("2-3-0-1"))
                                {
                                    var word1 = (ushort)(bytes[1] * 256 + bytes[0]);
                                    var word2 = (ushort)(bytes[3] * 256 + bytes[2]);
                                    data = new[] {word2, word1};
                                }
                            }
                            break;
                        }
                }

                while (!signalState.OutOfDateAlarm)
                {
                    lock (Locker)
                    {
                        try
                        {
                            if (!master.Open())
                            {
                                master.Close();
                                AddMessage("com port not opening", DebugLevel.ExceptionLevel);
                            }

                            if (signalState.UseByDate <= AdvDateTime.Now || !signalState.ChannelLink)
                            {
                                AddMessage(signalName + " useByDate <= Now or channelLink = false",
                                    DebugLevel.FullModeLevel);
                                break;
                            }

                            master.WriteMultipleRegisters(slaveAddress, register, data);
                            if (master.GetType() == typeof (ModbusASCII) || master.GetType() == typeof (ModbusRTU))
                                master.Close();

                            AddMessage(signalName + ": euValueBand [" + minEu + ", " + maxEu + "], " +
                                "rawValueBand [" + minRaw + ", " + maxRaw + "], rawValue: " +
                                rawValue + ", after conversion: " + value, DebugLevel.FullModeLevel);
                            signalState.ChannelLink = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            AddMessage("WriteMultipleRegisters catch exception: " + ex.Message, DebugLevel.ExceptionLevel);
                        }
                    }
                    PlcThread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                AddMessage(": an exception occurred in the function WriteMultipleRegistersThread() [ " +
                    ex.Message + " ]", DebugLevel.ExceptionLevel);
            }
        }
    }
}