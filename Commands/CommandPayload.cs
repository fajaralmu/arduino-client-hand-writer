using System;

namespace serial_communication_client.Commands
{

    public class CommandPayload
    {
        public CommandName Name { get; }
        
        // 0 -> Hardware PIN
        // 1 -> durationSec
        // 2 -> intervalSec
        // 3 -> angle
        readonly byte[] arguments;

        public int Size { get => 2 + arguments.Length; }
        public CommandPayload(CommandName name, params byte[] arguments)
        {
            Name = name;
            this.arguments = arguments;
        }
        public CommandPayload(CommandName name, HardwarePin hardware, params byte[] arguments)
        {
            Name = name;
            this.arguments = BuildArgument( hardware, arguments );
        }

        private byte[] BuildArgument(HardwarePin hardware, byte[] arguments)
        {
            // TODO: validate hardware pin
            byte[] result = new byte[arguments.Length + 1];
            result[0] = (byte) hardware;
            for (int i = 1; i < result.Length; i++)
            {
                result[i] = arguments[i - 1];
            }
            return result;
        }

        public byte[] Extract()
        {
            byte[] result = new byte[Size];
            result[0] = (byte)Name;
            result[1] = (byte)arguments.Length;
            for (int i = 2; i < Size; i++)
            {
                result[i] = arguments[i - 2];
            }
            return result;
        }
    }
}