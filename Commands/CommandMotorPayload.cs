using System;

namespace serial_communication_client.Commands
{
    public class CommandMotorPayload : CommandPayload
    {
        public const byte ANGLE_ADJUSTMENT = 0;
        public int Angle => _angle;
        private byte _angle;

        public static CommandMotorPayload NewCommand( HardwarePin pin, byte angle )
        {
            byte adjustedAngle = (byte)(angle + ANGLE_ADJUSTMENT);
            if (adjustedAngle > 250)
            {
                throw new ArgumentOutOfRangeException("angle");
            }
            return new CommandMotorPayload( pin, adjustedAngle );
        }
        private CommandMotorPayload( HardwarePin pin, byte angle) : base( CommandName.MOVE_SERVO, pin, 0,0,angle)
        {
            _angle = angle;
        }
    }

}