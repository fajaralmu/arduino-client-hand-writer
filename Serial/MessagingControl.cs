namespace serial_communication_client.Serial
{
    // https://en.wikipedia.org/wiki/C0_and_C1_control_codes
    public enum MessagingControl : int
    {
        SOH = 01,
        STX = 02,
        ETX = 03,
        EOT = 04,
        None = 0,

        // generated ONLY from this application, not serial
        Invalid = -1,
    }
}