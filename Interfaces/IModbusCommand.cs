namespace ModbusMasterIO.Interfaces
{
    internal interface IModbusCommand
    {
        event StateHandler OnMessage;
        bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints);
        void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value);
        void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value);
        void WriteMultipleCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints, bool[] data);
        void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data);
        bool Open();
        bool Close();
    }
}