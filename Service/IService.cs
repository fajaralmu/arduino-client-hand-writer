using serial_communication_client;

namespace MovementManager.Service
{
    public interface IService
    {
        bool Connected { get; }

        void MoveMotor( HardwarePin pin, byte angle, int waitDuration = 0 );
        int ReadMotorAngle( HardwarePin pin );
        void ToggleLed( HardwarePin pin, bool turnOn = true, int waitDuration = 0 );

        void Connect();
        void Close();

    }
    
}