using System;
using System.Collections.Generic;
using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using PlatformExtensions;

namespace ModbusMasterIO.Maps
{
    internal abstract class ReadMap : Map
    {
        protected ushort numberOfPoints;
        protected ushort startAddress = ushort.MaxValue;
        protected readonly ushort maxNumberReadRegistersAtTime;
        protected readonly List<MbSignalState> signalList = new List<MbSignalState>();
        protected readonly Dictionary<int, double> values = new Dictionary<int, double>();

        private readonly short registerShift;
        private ushort maxAddress = ushort.MinValue;

        protected ReadMap(byte id, IModbusCommand mbCommand, DeviceInfo device, ushort maxNumberReadRegisters,
            short shift) : base(id, mbCommand, device)
        {
            maxNumberReadRegistersAtTime = maxNumberReadRegisters;
            registerShift = shift;
        }

        public override sealed void InitMap()
        {
            try
            {
                BuildListMbSignals(deviceInfo);
                if (signalList.Count == 0)
                {
                    AddMessage("listSignals is empty", DebugLevel.LoadUnloadLevel);
                    return;
                }
                signalList.Sort();
                startAddress = (ushort)(startAddress + registerShift);
                maxAddress = (ushort)(maxAddress + registerShift);
                numberOfPoints = GetNumberOfPoints();

                AddMessage("map has " + signalList.Count + " signals, startAddress = " + startAddress + 
                    ", maxAddress = " + maxAddress + ", numberOfPoints = " + numberOfPoints,
                    DebugLevel.LoadUnloadLevel);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in InitMap(). " + ex.StackTrace, DebugLevel.LoadUnloadLevel);
            }
        }

        //количество сигналов которые надо читать
        private ushort GetNumberOfPoints()
        {
            return (ushort)(maxAddress - startAddress + GetChannelFromType());
        }

        //Возвращает количество регистров для разных типов переменных
        private ushort GetChannelFromType()
        {
            if (string.Equals(signalList[signalList.Count - 1].DataType, "dword") ||
                string.Equals(signalList[signalList.Count - 1].DataType, "float"))
                return 2;

            if (string.Equals(signalList[signalList.Count - 1].DataType, "double"))
                return 4;

            return 1;
        }

        //создаём список сигналов
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
                                if (mbSignalState.Register < startAddress)
                                    startAddress = mbSignalState.Register;

                                if (mbSignalState.Register > maxAddress)
                                    maxAddress = mbSignalState.Register;

                                mbSignalState.OnMessage += OnNewMessage;
                                signalList.Add(mbSignalState);
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

        private void OnNewMessage(object sender, MessageEventArgs e)
        {
            AddMessage(e.Message, e.CurrentLevel);
        }

        //читаем регистры
        public abstract void Read();

        //инициализируем сигналы значениями
        protected virtual void SetSignalValue()
        {
            try
            {
                foreach (var signal in signalList)
                {
                    int register = signal.Register + registerShift;
                    if (string.Equals(signal.DataType, "ushort") ||
                        string.Equals(signal.DataType, "short") ||
                        string.Equals(signal.DataType, "word"))
                    {
                        double word = values[register];
                        signal.SetValue(word);
                    }
                    else if (string.Equals(signal.DataType, "dword") || 
                        string.Equals(signal.DataType, "float"))
                    {
                        double word1 = values[register];
                        double word2 = values[register + 1];
                        signal.SetValue(word1, word2);
                    }
                    else if (string.Equals(signal.DataType, "double"))
                    {
                        double word1 = values[register];
                        double word2 = values[register + 1];
                        double word3 = values[register + 2];
                        double word4 = values[register + 3];
                        signal.SetValue(word1, word2, word3, word4);
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in SetSignalValue(). " + ex.StackTrace, DebugLevel.FullModeLevel);
            }
        }
    }
}