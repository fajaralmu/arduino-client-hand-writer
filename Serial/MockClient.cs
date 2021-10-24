using System;
using System.Collections.Generic;
using System.Threading;
using serial_communication_client;
using serial_communication_client.Commands;

namespace arduino_client_hand_writer.Serial
{
    public class MockClient : IClient
    {
        private IDictionary<HardwarePin, int> _motorAngles = new Dictionary<HardwarePin, int>();
        public MockClient()
        {

        }
        public void Connect()
        {

        }
        public void Close()
        {

        }
        public string Send(CommandPayload cmd, int waitDuration = 0)
        {
            if (waitDuration > 0)
            {
                Thread.Sleep(waitDuration);
            }
            // if move motor
            if (cmd is CommandMotorPayload)
            {
                CommandMotorPayload cmdMotor = (CommandMotorPayload)cmd;

                if (_motorAngles.ContainsKey(cmdMotor.Pin) == false)
                {
                    _motorAngles.Add(cmdMotor.Pin, cmdMotor.Angle);
                }
                else
                {
                    _motorAngles[cmdMotor.Pin] = cmdMotor.Angle;
                }
            }
            else if (cmd is CommanReadMotorPayload)
            {
                CommanReadMotorPayload cmdMotor = (CommanReadMotorPayload)cmd;

                Console.WriteLine("CommandName.READ_SERVO");
                if (_motorAngles.ContainsKey(cmdMotor.Pin) == false)
                {
                    _motorAngles.Add(cmdMotor.Pin, 0);
                    return 0.ToString();
                }
                return _motorAngles[cmdMotor.Pin].ToString();
            }
            else
            {
            }

            return 1.ToString();
        }
    }
}