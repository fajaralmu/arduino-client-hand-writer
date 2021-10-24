namespace serial_communication_client.Commands
{
    public class CommanReadMotorPayload : CommandPayload
    {
        public HardwarePin Pin {get;}
        public CommanReadMotorPayload( HardwarePin hardwarePin )
            : base ( CommandName.READ_SERVO, hardwarePin )
        {
            Pin = hardwarePin;
        }
    }
}