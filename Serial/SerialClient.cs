using System;
using System.IO.Ports;
using System.Threading;
using serial_communication_client.Commands;

namespace MovementManager.Serial
{
    public class SerialClient
    {
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly int _baudRate;

        private SerialClient( string portName, int baudRate )
        {
            _portName = portName;
            _baudRate = baudRate;
        }
        public static SerialClient Create( string portName, int baudRate )
        {
            return new SerialClient( portName, baudRate );
        }

        public void Connect()
        {
            if (_serialPort != null)
            {
                Close();
            }
            _serialPort = new SerialPort{
                PortName = _portName,
                BaudRate = _baudRate
            };
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();
        }

        public void Close()
        {
            if ( null != _serialPort && _serialPort.IsOpen )
            {
                _serialPort.Close();
                Console.Write("Close Serial Port");
            }
        }

        private void OnDataReceived( object sender, SerialDataReceivedEventArgs e )
        {
            string data = _serialPort.ReadLine();
         //   Console.WriteLine($"Serial Port { _portName } >> " + data);
        }

        public void Send( CommandPayload command, int waitDuration = 0 )
        {
            _serialPort.Write( command.Extract(), 0, command.Size );

            if (waitDuration > 0)
            {
                Thread.Sleep( waitDuration );
            }
        }
    }
}