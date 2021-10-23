namespace serial_communication_client.Serial
{
   
    public enum MessagingControl : int
    {
        SOH = 01,
        STX = 02,
        ETX = 03,
        EOT = 04,
        None = 0,
        Invalid = -1,
    }
}