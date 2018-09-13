using System;
using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using ModbusMasterIO.Maps;
using PlatformExtensions;
using PlcEnvironment;
using RuntimeTable;

namespace ModbusMasterIO.Functions
{
    internal class WriteSingleCoil : WriteMap
    {
        public WriteSingleCoil(byte id, IModbusCommand mbCommand, DeviceInfo device, object locker)
            : base(id, mbCommand, device, locker)
        {
            modbusMapType = ModbusMapType.WriteSingleCoil;
        }

        protected override void WriteFunction()
        {
            try
            {
                var signalState = (SignalState) PlcThread.GetState();
                var signalName = signalState.SignalName;
                var mbInfo = dicSignals[signalName];
                var register = mbInfo.Register;
                
                var value = Convert.ToBoolean(signalState.Value);
                if (mbInfo.Invert)
                    value = !value;

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

                            if (signalState.UseByDate < AdvDateTime.Now || !signalState.ChannelLink)
                            {
                                AddMessage(signalName + " useByDate < Now or ChannelLink = false", DebugLevel.FullModeLevel);
                                return;
                            }

                            master.WriteSingleCoil(slaveAddress, register, value);
                            if (master.GetType() == typeof(ModbusASCII) || master.GetType() == typeof(ModbusRTU))
                                master.Close();

                            AddMessage(signalName + " register: " + register + " set value: " + value, 
                                DebugLevel.FullModeLevel);
                            signalState.ChannelLink = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            AddMessage("WriteSingleCoil catch exception: " + ex.Message, DebugLevel.ExceptionLevel);
                            if (master.GetType() == typeof(ModbusASCII) || master.GetType() == typeof(ModbusRTU))
                                master.Close();
                        }
                    }
                    PlcThread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                AddMessage(": an exception occurred in the function WriteSingleCoilThread() [ " +
                    ex.Message + " ]", DebugLevel.ExceptionLevel);
            }
        }
    }
}