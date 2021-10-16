using System;

namespace serial_communication_client.Commands
{
    public class CommandMotorPayload : CommandPayload
    {
        const byte ADJUSTMENT = 8;
        public int Angle => _angle;
        private byte _angle;

        public static CommandMotorPayload NewCommand( byte pin, byte angle )
        {
            byte adjustedAngle = (byte)(angle + ADJUSTMENT);
            if (adjustedAngle > 250)
            {
                throw new ArgumentOutOfRangeException("angle");
            }
            return new CommandMotorPayload( pin, adjustedAngle );
        }
        private CommandMotorPayload(byte pin, byte angle) : base(CommandName.MOVE_SERVO, pin,0,0,angle)
        {
            _angle = angle;
        }
    }

}