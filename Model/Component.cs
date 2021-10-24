using MovementManager.Service;
using serial_communication_client;

namespace MovementManager.Model
{
    public abstract class Component
    {
        public HardwarePin Pin {get;}
        protected readonly IService _service;
        public Component( HardwarePin pin, IService service )
        {
            Pin = pin;
            _service = service;
        }
    }
}