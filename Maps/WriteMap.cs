using System;
using System.Collections.Generic;
using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using PlatformExtensions;
using PlcEnvironment;
using RuntimeTable;

namespace ModbusMasterIO.Maps
{
    internal abstract class WriteMap : Map
    {
        protected readonly object Locker;
        protected readonly LiveState liveState = new LiveState();
        protected readonly Dictionary<string, MbSignalState> dicSignals = new Dictionary<string, MbSignalState>();

        protected WriteMap(byte id, IModbusCommand mbCommand, DeviceInfo device, object locker) :
            base(id, mbCommand, device)
        {
            Locker = locker;
        }

        public override void InitMap()
        {
            try
            {
                BuildListMbSignals(deviceInfo);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in InitMap(). " + ex.StackTrace, DebugLevel.LoadUnloadLevel);
            }
        }

        protected override sealed void BuildListMbSignals(DeviceInfo mbDevice)
        {
            try
            {
                if (mbDevice.Devices == null || mbDevice.Devices.Count == 0)
                    return;

                foreach (var currentDevice in mbDevice.Devices)
                {
                    if (currentDevice.Signals != null && currentDevice.Signals.Count != 0)
                    {
                        foreach (KeyValuePair<string, SignalInfo> signal in currentDevice.Signals)
                        {
                            MbSignalState mbSignalState = new MbSignalState().GetInstance(signal);
                            if (mbSignalState != null && mbSignalState.FunctionNumber == Convert.ToUInt16(modbusMapType))
                            {
                                dicSignals.Add(mbSignalState.SignalName, mbSignalState);
                                liveState[mbSignalState.SignalName].OnValueSetting += OnValueSetting;
                            }
                        }
                    }
                    BuildListMbSignals(currentDevice);
                }
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in BuildListMbSignals(). " + ex.StackTrace, DebugLevel.LoadUnloadLevel);
            }
        }

        private void OnValueSetting(object sender, EventArgs e)
        {
            new PlcThread(WriteFunction, sender, "WriteFunction №" + Convert.ToUInt16(modbusMapType)).Start();
        }

        protected abstract void WriteFunction();
    }
}