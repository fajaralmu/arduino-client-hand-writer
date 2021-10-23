namespace serial_communication_client.Commands
{
    public class CommandLedPayload : CommandPayload
    {
        public int DurationMs => _durationSec * 1000;
        private byte _durationSec = 0;
        public CommandLedPayload(
            CommandName name, 
            HardwarePin hardwarePin,
            byte durationSec = 0,
            byte intervalSec = 0)
            : base (name, hardwarePin, durationSec, intervalSec)
        {
            _durationSec = durationSec;
        }
    }
}