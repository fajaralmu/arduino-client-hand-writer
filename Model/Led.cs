using MovementManager.Service;
using serial_communication_client;

namespace MovementManager.Model
{
    public class Led : Component
    {
        public Led(HardwarePin pin, IService service) : base(pin, service)
        {

        }

        public void Toggle(bool turnOn, int waitDuration = 0) =>
            _service.ToggleLed(Pin, turnOn, waitDuration);
    }
}