using System;
using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using ModbusMasterIO.Maps;
using PlatformExtensions;
using PlcEnvironment;
using RuntimeTable;

namespace ModbusMasterIO.Functions
{
    internal class WriteSingleRegister : WriteMap
    {
        public WriteSingleRegister(byte id, IModbusCommand mbCommand, DeviceInfo device, object locker)
            : base(id, mbCommand, device, locker)
        {
            modbusMapType = ModbusMapType.WriteSingleRegister;
        }

        protected override void WriteFunction()
        {
            try
            {
                var signalState = (SignalState)PlcThread.GetState();
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

                double value = signalState.Value;
                if (euValueBand && (value < minEu || value > maxEu))
                {
                    AddMessage(signalName + ": exceeded the permissible limits. Expect [" + minEu + "; " +
                        maxEu + "], try to write value [" + value + "]", DebugLevel.FullModeLevel);
                    return;
                }

                double rawValue = value;
                if (string.Equals(type, "word") && string.Equals(byteOrder, "1-0"))
                {
                    var word = value;
                    byte b1 = Convert.ToByte(word / 0x100);
                    byte b2 = Convert.ToByte(word % 0x100);
                    value = (ushort) (256*b2 + b1);
                    AddMessage("Invert byte order [1-0]. Raw value: " + rawValue + ". New value: " + value,
                        DebugLevel.FullModeLevel);
                }

                if (euValueBand && rawValueBand)
                    value = (value - minEu)/(maxEu - minEu)*(maxRaw - minRaw) + minRaw;
                value = Math.Round(value, 0);

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

                            master.WriteSingleRegister(slaveAddress, register, (ushort)Math.Round(value, 0));
                            if (master.GetType() == typeof(ModbusASCII) || master.GetType() == typeof(ModbusRTU))
                                master.Close();

                            AddMessage(signalName + " register: " + register + " set value: " + value, 
                                DebugLevel.FullModeLevel);
                            signalState.ChannelLink = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            AddMessage("WriteSingleRegister catch exception: " + ex.Message, DebugLevel.ExceptionLevel);
                            if (master.GetType() == typeof(ModbusASCII) || master.GetType() == typeof(ModbusRTU))
                                master.Close();
                        }
                    }
                    PlcThread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                AddMessage(": an exception occurred in the function WriteSingleRegisterThread() [ " +
                    ex.Message + " ]", DebugLevel.ExceptionLevel);
            }
        }
    }
}