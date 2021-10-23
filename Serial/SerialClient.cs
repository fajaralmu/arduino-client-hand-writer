using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using arduino_client_hand_writer.Serial;
using serial_communication_client.Commands;

namespace serial_communication_client.Serial
{
    public class SerialClient : IClient
    {
        const MessagingControl SOH = MessagingControl.StartOfHeader;
        const MessagingControl STX = MessagingControl.StartOfText;
        const MessagingControl ETX = MessagingControl.EndOfText;
        const MessagingControl EOT = MessagingControl.EndOfTransmision;

        const int DELAY_PER_WRITE = 100;
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly int _baudRate;

        const int BEGIN_RESPONSE = -1;
        const int END_RESPONSE = 256;
        const long WAITING_INTERVAL = 30 * 1000;

        private CommandName _currentCommandName = CommandName.NONE;
        private string _currentResponse;

        private MessagingControl _currentControlMode = MessagingControl.None;

        private bool hasResponse        = false;
        private bool responsePrinted    = false;
        private string responsePayload  = null;

        private DateTime lastReceived  = default(DateTime);

        private SerialClient(string portName, int baudRate)
        {
            _portName = portName;
            _baudRate = baudRate;
        }
        public static SerialClient Create(string portName, int baudRate)
        {
            return new SerialClient(portName, baudRate);
        }

        public void Connect()
        {
            if (_serialPort != null)
            {
                Close();
            }
            _serialPort = new SerialPort
            {
                PortName = _portName,
                BaudRate = _baudRate
            };
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();
        }

        public void Close()
        {
            if (null != _serialPort && _serialPort.IsOpen)
            {
                _serialPort.Close();
                Log("Close Serial Port");
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lastReceived = DateTime.Now;

            string data = _serialPort.ReadLine().Trim();
            bool validCode = Enum.TryParse<MessagingControl>(data, out MessagingControl control) && Enum.IsDefined<MessagingControl>(control);
            if (validCode)
            {
                LogFromSerial($"Control: {control} ({ data }) ");
            }
            if (validCode)
            {
                if (_currentControlMode == MessagingControl.None && control != SOH )
                {   
                    Console.WriteLine($"<!> Invalid response control: { control }. Current control: { _currentControlMode }. Expected control = { SOH } ");
                    // treated as Invalid
                    validCode = false;
                } else {
                    _currentControlMode = control;
                    return;
                }
            }
            
            if (!validCode)
            {
                switch (_currentControlMode)
                {
                    case SOH:
                        Enum.TryParse<CommandName>(data, out CommandName commandName);
                        if (commandName == _currentCommandName)
                        {
                            hasResponse = true;
                            LogFromSerial("(Response Header) " + commandName);
                        }
                        break;
                    case STX:
                        if (hasResponse)
                        {
                            responsePrinted = true;
                            responsePayload = data;
                            LogFromSerial("(Response Payload) " + data);
                        }
                        break;
                    default:
                        break;
                }
            }


        }

        private void LogFromSerial(string value)
        {
            Log($"[Data] { value }");
        }
        private void Log(string value)
        {
            Debug.WriteLine($"Serial Port { _portName } >> { value }");
        }
        public string Send(CommandPayload command, int waitDuration = 0)
        {
            Reset();
            _currentCommandName = command.Name;
            Log("[Start Command] " + command.Name);
            _serialPort.Write(command.Extract(), 0, command.Size);

            if (waitDuration > 0)
            {
                Thread.Sleep(waitDuration);
            }

            Thread.Sleep(DELAY_PER_WRITE);

            bool responseReceived = WaitForResponse();

            if (responseReceived)
            {
                Log($"[Response]: '{ responsePayload }'");
                Log($"[End Command] { command.Name }");
                
                return responsePayload;
            }
            else
            {
                throw new TimeoutException($"Response timeout while executing { _currentCommandName }");
            }
        }

        private bool WaitForResponse()
        {
            long startedWaiting = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while(!hasResponse)
            {
                // check waiting time (ms)
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (now - startedWaiting > WAITING_INTERVAL)
                {
                    // timeout error
                    return false;
                }
            }

            return true;
        }

        private void Reset()
        {
            Log("");
            Log("___RESET_RESPONSE_DATA___");
            _currentControlMode = MessagingControl.None;
            _currentCommandName = CommandName.UNDEFINED;
            hasResponse = false;
            responsePrinted = false;
            responsePayload = null;
        }
    }
}
