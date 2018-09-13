using SystemTable.SystemTools;
using ModbusMasterIO.Interfaces;

namespace ModbusMasterIO.Maps
{
    internal abstract class Map
    {
        public event StateHandler OnMessage;

        protected readonly byte slaveAddress;
        protected readonly IModbusCommand master;
        protected readonly DeviceInfo deviceInfo;
        protected ModbusMapType modbusMapType = ModbusMapType.ReadInputs;

        protected Map(byte id, IModbusCommand mbCommand, DeviceInfo device)
        {
            slaveAddress = id;
            master = mbCommand;
            deviceInfo = device;
        }

        public abstract void InitMap();

        protected abstract void BuildListMbSignals(DeviceInfo mbDevice);

        protected void AddMessage(string messageObject, int currentLevel)
        {
            var e = new MessageEventArgs(modbusMapType + ": " + messageObject, currentLevel);
            StateHandler handler = OnMessage;
            if (handler != null)
                handler(this, e);
        }
    }
}