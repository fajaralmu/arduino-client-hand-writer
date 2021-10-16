namespace serial_communication_client.Commands
{

    public class CommandPayload
    {
        readonly CommandName name;
        
        // 0 -> Hardware PIN
        // 1 -> durationSec
        // 2 -> intervalSec
        // 3 -> angle
        readonly byte[] arguments;

        public int Size { get => 2 + arguments.Length; }
        public CommandPayload(CommandName name, params byte[] arguments)
        {
            this.name = name;
            this.arguments = arguments;
        }

        public byte[] Extract()
        {
            byte[] result = new byte[Size];
            result[0] = (byte)name;
            result[1] = (byte)arguments.Length;
            for (int i = 2; i < Size; i++)
            {
                result[i] = arguments[i - 2];
            }
            return result;
        }
    }
}