using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;
using ModbusMasterIO.Maps;
using PlatformExtensions;

namespace ModbusMasterIO.Functions
{
    internal class ReadHoldingRegisters : ReadMap
    {
        public ReadHoldingRegisters(byte id, IModbusCommand mbCommand, DeviceInfo device, ushort maxNumberReadRegisters,
            short shift) : base(id, mbCommand, device, maxNumberReadRegisters, shift)
        {
            modbusMapType = ModbusMapType.ReadHoldingRegisters;
        }

        public override void Read()
        {
            if (signalList.Count == 0)
                return;

            ushort countRegistersRead = 0;
            ushort address = startAddress;
            ushort channelCount = maxNumberReadRegistersAtTime;
            if (channelCount == 0)
                channelCount = numberOfPoints;

            values.Clear();
            while (countRegistersRead < numberOfPoints)
            {
                ushort[] inputs = master.ReadHoldingRegisters(slaveAddress, address, channelCount);
                if (inputs == null) return;
                AddMessage("startAddress = " + address + ", channelCount = " + channelCount, DebugLevel.FullModeLevel);

                foreach (var input in inputs)
                {
                    AddMessage("address = " + address + ", value = " + input, DebugLevel.FullModeLevel);
                    values.Add(address, input);

                    //сдвигаем адрес
                    address++;

                    //увеливаем счётчик прочитанных регистров
                    countRegistersRead++;
                }
            }
            SetSignalValue();
        }
    }
}