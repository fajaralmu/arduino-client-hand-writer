namespace serial_communication_client.Serial
{
   
    public enum MessagingControl : int
    {
        StartOfHeader   = 01,
        StartOfText     = 02,
        EndOfText       = 03,
        EndOfTransmision= 04,
        None            = -1,
    }
}