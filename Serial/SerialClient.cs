using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using arduino_client_hand_writer.Serial;
using serial_communication_client.Commands;
using System.Linq;

namespace serial_communication_client.Serial
{
    public class SerialClient : IClient
    {
        const MessagingControl SOH = MessagingControl.SOH;
        const MessagingControl STX = MessagingControl.STX;
        const MessagingControl ETX = MessagingControl.ETX;
        const MessagingControl EOT = MessagingControl.EOT;

        const int DELAY_PER_WRITE = 1;
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly bool _showRawData;

        const int BEGIN_RESPONSE = -1;
        const int END_RESPONSE = 256;
        const long WAITING_INTERVAL = 30 * 1000;

        private CommandName _currentCommandName = CommandName.NONE;
        private string _currentResponse;

        private MessagingControl _currentControlMode = MessagingControl.None;

        private bool hasResponse        = false;
        private bool endOfResponse      = false;
        private bool responsePrinted    = false;
        private string responsePayload  = null;

        private DateTime lastReceived  = default(DateTime);

        private SerialClient(string portName, int baudRate, bool showRawData = false)
        {
            _portName = portName;
            _baudRate = baudRate;
            _showRawData = showRawData;
        }
        public static SerialClient Create(string portName, int baudRate, bool showRawData = false )
        {
            return new SerialClient(portName, baudRate, showRawData);
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
            
            if ( _showRawData )
            {
                Console.WriteLine("{raw}" + data);
            }

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            bool validCode = ValidateControlValue( data, out MessagingControl control );
            if (validCode)
            {
                LogFromSerial($"@{control}");
            }
            if (validCode)
            {
                if (_currentControlMode == MessagingControl.None && control != SOH )
                {   
                    Console.WriteLine($"<!> Invalid response control: { control }. Current control: { _currentControlMode }. Expected control = { SOH } ");
                    // treated as Invalid
                 //   validCode = false;
                    return;
                } else {
                    if (control == EOT)
                    {
                        endOfResponse = true;
                    }

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

        private bool ValidateControlValue(string data, out MessagingControl control)
        {
            control = MessagingControl.Invalid;
            MessagingControl[] result = Enum.GetValues<MessagingControl>().Where(e => e.ToString().Equals( data )).ToArray();
            if (result.Length == 0)
            {
                return false;
            }
            control = result.First();
            return true;
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
            long startCommand = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            _serialPort.Write(command.Extract(), 0, command.Size);

            if (waitDuration > 0)
            {
                Thread.Sleep(waitDuration);
            }

            Thread.Sleep(DELAY_PER_WRITE);

            bool responseReceived = WaitForResponse( out long waitingForResponseDuration );
            long commandDuration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startCommand;;
            if (responseReceived)
            {
                Log($"[Response]: '{ responsePayload }'");
                Log($"[End Command] { command.Name } - { commandDuration } ms, defined_delay:{ waitDuration } ms, waiting: { waitingForResponseDuration} ms");
                
                return responsePayload;
            }
            else
            {
                throw new TimeoutException($"Response timeout while executing { _currentCommandName }. Waiting time: { commandDuration } ms");
            }
        }

        private bool WaitForResponse( out long duration )
        {
            duration = 0;
            long startedWaiting = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while( !endOfResponse )
            {
                // Console.WriteLine(" ..Wait.. ");
                // check waiting time (ms)
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (now - startedWaiting > WAITING_INTERVAL)
                {
                    // timeout error
                    return false;
                }
            }
            duration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startedWaiting;
            return true;
        }

        private void Reset()
        {
            Log("");
            Log("___RESET_RESPONSE_DATA___");
            _currentControlMode = MessagingControl.None;
            _currentCommandName = CommandName.UNDEFINED;
            endOfResponse = false;
            hasResponse = false;
            responsePrinted = false;
            responsePayload = null;
        }
    }
}
