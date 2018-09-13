using System;
using System.Net;
using System.Net.Sockets;
using Modbus.Device;
using PlatformExtensions;

namespace ModbusMasterIO.Interfaces
{
    internal class ModbusTCP : IModbusCommand
    {
        private ModbusIpMaster master;
        private TcpClient tcpClient;
        private bool status;
        private readonly string deviceIp;
        private readonly int port;
        public event StateHandler OnMessage;

        public ModbusTCP(string devIp, int devPort)
        {
            deviceIp = devIp;
            port = devPort;
        }

        public bool Open()
        {
            return true;
        }

        private void Connect()
        {
            if (deviceIp == "" || port <= 0)
            {
                AddMessage(deviceIp + " not connected, do not specify the device address or port",
                    DebugLevel.FullModeLevel);
                status = false;
                return;
            }

            try
            {
                tcpClient = new TcpClient();
                IPAddress ipAddress = IPAddress.Parse(deviceIp);
                IPEndPoint remoteEp = new IPEndPoint(ipAddress, port);
                tcpClient.Connect(remoteEp);
                master = ModbusIpMaster.CreateIp(tcpClient);
                status = true;
            }
            catch (SocketException ex)
            {
                AddMessage(deviceIp + " not connected. " + ex.Message, DebugLevel.FullModeLevel);
                status = false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                AddMessage(deviceIp + " not connected. " + ex.Message, DebugLevel.FullModeLevel);
                status = false;
            }
            catch (Exception ex)
            {
                AddMessage(deviceIp + " not connected. " + ex.Message, DebugLevel.FullModeLevel);
                status = false;
            }
        }

        public bool Close()
        {
            status = false;
            try
            {
                if (tcpClient != null && tcpClient.Client.Connected)
                {
                    tcpClient.GetStream().Close();
                    AddMessage("Stream closed", DebugLevel.FullModeLevel);

                    tcpClient.Client.Close();
                    AddMessage("TcpClient.Client closed", DebugLevel.FullModeLevel);

                    tcpClient.Close();
                    AddMessage("TcpClient closed", DebugLevel.FullModeLevel);
                }
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
                AddMessage("Catch exception in function Dispose(). " + ex.Message, DebugLevel.ExceptionLevel);
            }
        }

        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!status)
                Connect();

            try
            {
                return master.ReadCoils(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in function ReadCoils(). " + ex.Message, DebugLevel.ExceptionLevel);
                return null;
            }
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!status)
                Connect();

            try
            {
                return master.ReadInputs(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in function ReadInputs(). " + ex.Message, DebugLevel.ExceptionLevel);
                return null;
            }
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!status)
                Connect();

            try
            {
                return master.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in function ReadHoldingRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                return null;
            }
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            if (!status)
                Connect();

            try
            {
                return master.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in function ReadInputRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
                return null;
            }
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            if (!status)
                Connect();

            try
            {
                master.WriteSingleCoil(slaveAddress, coilAddress, value);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in function WriteSingleCoil(). " + ex.Message, DebugLevel.ExceptionLevel);
            }
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            if (!status)
                Connect();

            try
            {
                master.WriteSingleRegister(slaveAddress, registerAddress, value);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in the function WriteSingleRegister(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
            }
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints, bool[] data)
        {
            if (!status)
                Connect();

            try
            {
                master.WriteMultipleCoils(slaveAddress, startAddress, data);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in the function WriteMultipleCoils(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
            }
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            if (!status)
                Connect();

            try
            {
                master.WriteMultipleRegisters(slaveAddress, startAddress, data);
            }
            catch (Exception ex)
            {
                status = false;
                AddMessage("Catch exception in the function WriteMultipleRegisters(). " + ex.Message,
                    DebugLevel.ExceptionLevel);
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