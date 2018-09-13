using System;
using System.IO.Ports;
using Modbus.Device;
using PlatformExtensions;

namespace ModbusMasterIO.Interfaces
{
    internal class ModbusRTU : IModbusCommand
    {
        private readonly SerialPort serialPort;
        private readonly ModbusSerialMaster master;
        public event StateHandler OnMessage;

        public ModbusRTU(string portName, int baudRate, string parity, int dataBit, string stBit)
        {
            Parity masterParity = Parity.None;
            if (parity == "Odd")
                masterParity = Parity.Odd;
            if (parity == "Even")
                masterParity = Parity.Even;

            StopBits stopBit = StopBits.None;
            if (stBit == "One")
                stopBit = StopBits.One;
            if (stBit == "Two")
                stopBit = StopBits.Two;

            serialPort = new SerialPort(portName, baudRate, masterParity, dataBit, stopBit);
            master = ModbusSerialMaster.CreateRtu(serialPort);
            master.Transport.ReadTimeout = 500;
        }

        public bool Open()
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    AddMessage("Comport opened success", DebugLevel.FullModeLevel);
                }
                return true;
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function Open(). " + ex.Message, DebugLevel.FullModeLevel);
                return false;
            }
        }

        public bool Close()
        {
            try
            {
                serialPort.Close();
                AddMessage("Comport closed success", DebugLevel.FullModeLevel);
                return true;
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function Close(). " + ex.Message, DebugLevel.FullModeLevel);
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function Dispose(). " + ex.Message, 3);
            }
        }

        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                return master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function ReadCoils(). " + ex.Message, DebugLevel.ExceptionLevel);
                Close();
                return null;
            }
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                return master.ReadInputs(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function ReadInputs(). " + ex.Message, DebugLevel.ExceptionLevel);
                Close();
                return null;
            }
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                return master.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function ReadHoldingRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
                return null;
            }
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                return master.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function ReadInputRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
                return null;
            }
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                master.WriteSingleCoil(slaveAddress, coilAddress, value);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in function WriteSingleCoil(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
            }
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                master.WriteSingleRegister(slaveAddress, registerAddress, value);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in the function WriteSingleRegister(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
            }
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints, bool[] data)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                master.WriteMultipleCoils(slaveAddress, startAddress, data);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in the function WriteMultipleCoils(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
            }
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            if (!serialPort.IsOpen)
                Open();

            try
            {
                master.WriteMultipleRegisters(slaveAddress, startAddress, data);
            }
            catch (Exception ex)
            {
                AddMessage("Catch exception in the function WriteMultipleRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                Close();
            }
        }

        public void AddMessage(string messageObject, int currentLevel)
        {
            var e = new MessageEventArgs(messageObject, currentLevel);
            StateHandler handler = OnMessage;
            if (handler != null) handler(this, e);
        }
    }
}