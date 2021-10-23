namespace serial_communication_client.Commands
{
    public class CommanReadMotorPayload : CommandPayload
    {
        public CommanReadMotorPayload( HardwarePin hardwarePin )
            : base ( CommandName.READ_SERVO, hardwarePin )
        {
            
        }
    }
}