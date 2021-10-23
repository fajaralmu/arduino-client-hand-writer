using System;
using System.Threading;
using arduino_client_hand_writer.Serial;
using serial_communication_client;
using serial_communication_client.Commands;

namespace  MovementManager.Service
{
    public class ServiceImpl : IService
    {
        private readonly IClient _client;

        public ServiceImpl(IClient client)
        {
            _client = client;
        }

        public void Close()
        {
            _client.Close();
        }

        public void Connect()
        {
            _client.Connect();
        }

        public void MoveMotor(HardwarePin pin, byte angle, int waitDuration = 0)
        { 
            CommandMotorPayload command = CommandMotorPayload.NewCommand( pin, angle );
            _client.Send( command, 0 );

            Console.WriteLine("--check servo angle--");

            int latestAngle = ReadMotorAngle( pin );

            while( latestAngle != angle )
            {
                Console.WriteLine("wait latest angle: "+ latestAngle);
                latestAngle = ReadMotorAngle( pin );
            }

            Console.WriteLine($"END MOVE SERVO { pin } to angle: { angle }, waiting for { waitDuration } ms");
            Thread.Sleep( waitDuration );
        }

        public int ReadMotorAngle(HardwarePin pin)
        {
            string response = _client.Send( new CommanReadMotorPayload( pin ));

            return int.Parse( response ) - CommandMotorPayload.ANGLE_ADJUSTMENT;;
        }

        public void ToggleLed(HardwarePin pin, bool turnOn = true, int waitDuration = 0)
        {
            CommandLedPayload cmd = new CommandLedPayload( turnOn ? CommandName.LED_ON : CommandName.LED_OFF, pin );
            _client.Send( cmd, waitDuration );
        }
    }
}