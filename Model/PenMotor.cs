using MovementManager.Service;
using serial_communication_client;

namespace MovementManager.Model
{
    public class PenMotor:Motor
    {
        public byte PenDownAngle {get;}
        public byte PenUpAngle {get;}
        public int PenDownWaitDuration{get;set;}
        public int PenUpWaitDuration{get;set;}
        public PenMotor(
            HardwarePin pin, 
            IService service,
            byte pinDownAngle, 
            byte pinUpAngle ) : base(pin, service)
        {
            PenDownAngle = pinDownAngle;
            PenUpAngle = pinUpAngle;
        }

        public void PenDown() => Move(PenDownAngle, PenDownWaitDuration);
        public void PenUp() => Move(PenUpAngle, PenUpWaitDuration);

    }
}