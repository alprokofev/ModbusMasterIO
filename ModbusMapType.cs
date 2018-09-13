namespace ModbusMasterIO
{
    internal enum ModbusMapType
    {
        ReadCoils = 1,
        ReadInputs = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,
        WriteSingleCoil = 5,
        WriteSingleRegister = 6,
        WriteMultipleCoils = 15,
        WriteMultipleRegisters = 16
    }
}