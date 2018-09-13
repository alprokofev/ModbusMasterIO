using System;
using SystemTable.SystemTools;
using DeviceIO;
using ModbusMasterIO.Functions;
using ModbusMasterIO.Interfaces;
using ModbusMasterIO.Maps;
using PlatformExtensions;
using PlcEnvironment;

namespace ModbusMasterIO
{
    public class MasterIO : PlatformDevice
    {
        private DeviceInfo device;
        private IModbusCommand modbusCommand;
        private string protocol = "ModbusTCP";
        private ushort maxNumberReadRegistersAtTime;
        private short registerShift;
        private const ushort CONNECTION_TIMEOUT = 20;
        private long workThreadTickCount = AdvDateTime.TotalEnvironmentTickCount;

        //---------- ModbusTCP ---------------
        private string ipAddress = "192.168.0.1";
        private int port = 502;

        //--------- ModbusRTU/Ascii ----------
        private string portName = "COM1";
        private int baudRate = 9600;
        private string parity = "None";
        private int dataBits = 8;
        private string stopBits = "One";
        private byte slaveId = 1;

        private ReadMap readInputsReadMap;
        private ReadMap readCoilsReadMap;
        private ReadMap readInputRegisters;
        private ReadMap readHoldingRegisters;

        private WriteMap writeSingleCoil;
        private WriteMap writeSingleRegister;
        private WriteMap writeMultipleCoils;
        private WriteMap writeMultipleRegisters;

        public MasterIO()
        {
            DeviceDebugLevel = DebugLevel.LoadUnloadLevel;
            DeviceLogFile = "IO_ModbusMaster.log";
            TimeDelay = 250;
        }

        public override bool CustomInit(DeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
                return false;

            try
            {
                device = deviceInfo;
                LoadSettings();
                if (protocol == "modbustcp")
                    modbusCommand = new ModbusTCP(ipAddress, port);

                if (protocol == "modbusrtu")
                    modbusCommand = new ModbusRTU(portName, baudRate, parity, dataBits, stopBits);

                if (protocol == "modbusascii")
                    modbusCommand = new ModbusASCII(portName, baudRate, parity, dataBits, stopBits);

                modbusCommand.OnMessage += modbusCommand_OnMessage;

                readInputsReadMap = new ReadInputs(slaveId, modbusCommand, device, maxNumberReadRegistersAtTime, 
                    registerShift);
                readInputsReadMap.OnMessage += OnNewMessage;
                readInputsReadMap.InitMap();

                readCoilsReadMap = new ReadCoils(slaveId, modbusCommand, device, maxNumberReadRegistersAtTime,
                    registerShift);
                readCoilsReadMap.OnMessage += OnNewMessage;
                readCoilsReadMap.InitMap();

                readInputRegisters = new ReadInputRegisters(slaveId, modbusCommand, device, maxNumberReadRegistersAtTime,
                    registerShift);
                readInputRegisters.OnMessage += OnNewMessage;
                readInputRegisters.InitMap();

                readHoldingRegisters = new ReadHoldingRegisters(slaveId, modbusCommand, device, maxNumberReadRegistersAtTime,
                    registerShift);
                readHoldingRegisters.OnMessage += OnNewMessage;
                readHoldingRegisters.InitMap();

                writeSingleCoil = new WriteSingleCoil(slaveId, modbusCommand, device, Locker);
                writeSingleCoil.OnMessage += OnNewMessage;
                writeSingleCoil.InitMap();

                writeSingleRegister = new WriteSingleRegister(slaveId, modbusCommand, device, Locker);
                writeSingleRegister.OnMessage += OnNewMessage;
                writeSingleRegister.InitMap();

                writeMultipleCoils = new WriteMultipleCoils(slaveId, modbusCommand, device, Locker);
                writeMultipleCoils.OnMessage += OnNewMessage;
                writeMultipleCoils.InitMap();

                writeMultipleRegisters = new WriteMultipleRegisters(slaveId, modbusCommand, device, Locker);
                writeMultipleRegisters.OnMessage += OnNewMessage;
                writeMultipleRegisters.InitMap();

                new PlcThread(CustomWorkThread, null, "ModbusMasterIO_CustomWorkThread").Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void modbusCommand_OnMessage(object sender, MessageEventArgs e)
        {
            AddMessage(e.Message, e.CurrentLevel);
        }

        private void LoadSettings()
        {
            if (device.SourceArgs == null || device.SourceArgs.Count == 0)
                return;

            foreach (var sourceArg in device.SourceArgs)
            {
                switch (sourceArg.ArgName)
                {
                    case "Protocol":
                        {
                            protocol = sourceArg.ArgValue;
                            protocol = protocol.ToLower();
                            break;
                        }
                    case "IPAddress":
                        {
                            ipAddress = sourceArg.ArgValue;
                            break;
                        }
                    case "Port":
                        {
                            try
                            {
                                port = Convert.ToInt32(sourceArg.ArgValue);
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    case "SlaveId":
                        {
                            try
                            {
                                slaveId = Convert.ToByte(sourceArg.ArgValue);
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    case "PortName":
                        {
                            portName = sourceArg.ArgValue;
                            break;
                        }
                    case "BaudRate":
                        {
                            try
                            {
                                baudRate = Convert.ToInt32(sourceArg.ArgValue);
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    case "Parity":
                        {
                            parity = sourceArg.ArgValue;
                            break;
                        }
                    case "DataBits":
                        {
                            dataBits = Convert.ToInt32(sourceArg.ArgValue);
                            break;
                        }
                    case "StopBits":
                        {
                            stopBits = sourceArg.ArgValue;
                            break;
                        }
                    case "MaxNumberReadRegistersAtTime":
                        {
                            try
                            {
                                maxNumberReadRegistersAtTime = Convert.ToUInt16(sourceArg.ArgValue);
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    case "RegisterShift":
                        {
                            try
                            {
                                registerShift = Convert.ToInt16(sourceArg.ArgValue);
                                break;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                }
            }

            if (protocol == "modbustcp")
            {
                AddMessage(device.Name + " uploaded the following settings: DeviceLogFile: " + DeviceLogFile + ", DeviceDebugLevel: " +
                    DeviceDebugLevel + ", TimeDelay: " + TimeDelay + ", protocol: ModbusTcp" +
                    ", slaveId: " + slaveId + ", IPAddress: " + ipAddress + ", Port: " + port +
                    ", MaxNumberReadRegistersAtTime: " + maxNumberReadRegistersAtTime +
                    ", RegisterShift: " + registerShift, DebugLevel.LoadUnloadLevel);
            }
            if (protocol == "modbusrtu")
            {
                Locker = LockResource.GetLocker(portName);
                AddMessage(device.Name + " uploaded the following settings: DeviceLogFile: " + DeviceLogFile + ", DeviceDebugLevel: " +
                    DeviceDebugLevel + ", TimeDelay: " + TimeDelay + ", protocol: ModbusRtu" +
                    ", slaveId: " + slaveId + ", PortName: " + portName + ", BaudRate: " + baudRate +
                    ", Parity: " + parity + ", DataBits:  " + dataBits + ", StopBits: " + stopBits +
                    ", MaxNumberReadRegistersAtTime: " + maxNumberReadRegistersAtTime +
                    ", RegisterShift: " + registerShift, DebugLevel.LoadUnloadLevel);
            }
            if (protocol == "modbusascii")
            {
                Locker = LockResource.GetLocker(portName);
                AddMessage(device.Name + " uploaded the following settings: DeviceLogFile: " + DeviceLogFile + ", DeviceDebugLevel: " +
                    DeviceDebugLevel + ", TimeDelay: " + TimeDelay + ", protocol: ModbusAscii" +
                    ", slaveId: " + slaveId + ", PortName: " + portName + ", BaudRate: " + baudRate +
                    ", Parity: " + parity + ", DataBits: " + dataBits + ", StopBits: " + stopBits +
                    ", MaxNumberReadRegistersAtTime: " + maxNumberReadRegistersAtTime +
                    ", RegisterShift: " + registerShift, DebugLevel.LoadUnloadLevel);
            }
            AddMessage("", DebugLevel.LoadUnloadLevel);
        }

        public override bool CustomWork()
        {
            try
            {
                workThreadTickCount = AdvDateTime.TotalEnvironmentTickCount + CONNECTION_TIMEOUT*1000;
                long tickCountDevice = AdvDateTime.TotalEnvironmentTickCount;
                
                if (DeviceDebugLevel >= DebugLevel.TimeAnalyzingLevel)
                    AddMessage("Start scaning device", DebugLevel.TimeAnalyzingLevel);

                if (!modbusCommand.Open())
                {
                    modbusCommand.Close();
                    if (DeviceDebugLevel >= DebugLevel.FullModeLevel)
                        AddMessage(portName + " not opening", DebugLevel.FullModeLevel);
                    return false;
                }
                readInputsReadMap.Read();
                readCoilsReadMap.Read();
                readInputRegisters.Read();
                readHoldingRegisters.Read();

                if (modbusCommand.GetType() == typeof(ModbusASCII) || modbusCommand.GetType() == typeof(ModbusRTU))
                    modbusCommand.Close();

                if (DeviceDebugLevel >= DebugLevel.TimeAnalyzingLevel)
                    AddMessage(device.Name + " completed for " + ((AdvDateTime.TotalEnvironmentTickCount - 
                        tickCountDevice) / 1000.0), DebugLevel.TimeAnalyzingLevel);
            }
            catch (Exception ex)
            {
                AddMessage(device.Name + ": an exception occurred in the function CustomWork(): " +
                    ex.Message, DebugLevel.ExceptionLevel);

                if (modbusCommand.GetType() == typeof(ModbusASCII) || modbusCommand.GetType() == typeof(ModbusRTU))
                    modbusCommand.Close();
                return false;
            }
            AddMessage("=============================", DebugLevel.TimeAnalyzingLevel);
            return true;
        }

        private void CustomWorkThread()
        {
            while (true)
            {
                try
                {
                    PlcThread.Sleep(TimeDelay);
                    if (workThreadTickCount <= AdvDateTime.TotalEnvironmentTickCount)
                    {
                        modbusCommand.Close();
                        PlcThread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    AddMessage(device.Name + ": an exception occurred in the function CustomWorkThread(): " + ex.Message,
                        DebugLevel.ExceptionLevel);
                }
            }
        }

        private void OnNewMessage(object sender, MessageEventArgs e)
        {
            AddMessage(e.Message, e.CurrentLevel);
        }
    }
}