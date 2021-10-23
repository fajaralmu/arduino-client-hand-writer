using serial_communication_client.Commands;

namespace arduino_client_hand_writer.Serial
{
    public interface IClient
    {
         public void Connect();
         public void Close();
         public string Send( CommandPayload cmd, int waitDuration = 0 );
    }
}