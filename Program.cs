using System;
using System.Threading;
using MovementManager.Helper;
using MovementManager.Model;
using MovementManager.Serial;
using serial_communication_client;
using serial_communication_client.Commands;

namespace MovementManager
{
    class Program
    {
      
        const double PX_PER_CM = 37.795280352161;
        const int MAX_DEGREE = 180;
        const int B1_LENGTH = (int)( 15.5 * PX_PER_CM );
        const int B2_LENGTH = (int)( 13.5 * PX_PER_CM );
        const int PEN_FREE_ANGLE = 30;
        const double TOLERANCE = 0.5;

        static double verticalLength, horizontalLength;
        static SerialClient client;
        static void Main(string[] args)
        {
            client = SerialClient.Create( "COM7", 9600 );
            client.Connect();

            ResetMotor();
          //  if (true)return;
            verticalLength = VerticalLength();
            horizontalLength = HorizontalLength(); 
            double maxX = (horizontalLength - B1_LENGTH);
             for (int y = B1_LENGTH; y < verticalLength; y++)
            {
                for (int x = 0; x < maxX; x++)
                {
                    MovementProperty prop =  Move( x, y );
                    Console.WriteLine( $"x: { x }, y: { y }, alpha: { prop.Alpha }, beta: { prop.Beta }" );
                }
                Console.WriteLine("_______________");
            }
            Console.WriteLine(" ======== END ========= ");
            Console.ReadLine();
            client.Close();

        }

        static bool ValidateX( int x )
        {
            double maxX = (horizontalLength - B1_LENGTH);
            bool result = x >= 0 && x <= maxX;
         //   Console.WriteLine($"X = { x } min: { 0 } max: { maxX } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid X: { x }");
            }

            return result;
        }
        static bool ValidateY( int y )
        {
            bool result = y >= B1_LENGTH && y <= verticalLength;
        //    Console.WriteLine($"Y = { y } min: { B1_LENGTH } max: { verticalLength } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid Y: { y }");
            }

            return result;
        }

        static double VerticalLength()
        {
            return ( B1_LENGTH! + B2_LENGTH ) *  MathHelper.Cos( 45 );
        }
        static double HorizontalLength()
        {
            return B1_LENGTH * MathHelper.Cos( 45) + B2_LENGTH *  MathHelper.Cos( 45 );
        }
       
        ///////////////////////////// Movement Model //////////////////////////////

        static MovementProperty Move( int x, int y )
        {
            ValidateX( x );
            ValidateY( y );
            MovementProperty prop = CalculateMovement( x, y );
            if (null == prop)
            {
                throw new ArgumentException($"Point not feasible: {x}, {y}");
            }
            MoveMotor( prop );
            return prop;
        }

        private static void MoveMotor(MovementProperty prop)
        {
            Console.WriteLine(" move motor ");
            double alpha = prop.Alpha;
            double tetha = CalculateTetha( alpha, prop.Beta );

            // Move arms
            CommandMotorPayload cmdAlpha = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_A_PIN, (byte) alpha );
            CommandMotorPayload cmdTetha = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_B_PIN, (byte) tetha );
            client.Send( cmdAlpha, 500 );
            client.Send( cmdTetha, 1000 );

            // Move pen DOWN
            CommandMotorPayload cmdPenDown = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_PEN_PIN, 0 );
            client.Send( cmdPenDown, 700 );
            CommandMotorPayload cmdPenUp = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_PEN_PIN, PEN_FREE_ANGLE );
            client.Send( cmdPenUp, 500 );
        }

        static void ResetMotor()
        {
            // Reset arms
            CommandMotorPayload cmdAlpha = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_A_PIN, 0 );
            CommandMotorPayload cmdTetha = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_B_PIN, 0 );
            client.Send( cmdAlpha, 500 );
            client.Send( cmdTetha, 500 );

            // Reset pen
            CommandMotorPayload cmdPen = CommandMotorPayload.NewCommand( HardwarePin.MOTOR_PEN_PIN, PEN_FREE_ANGLE );
            client.Send( cmdPen, 500 );
        }

        static MovementProperty CalculateMovement( int x, int y )
        {
            
            for (double alpha = 0; alpha < 180; alpha++)
            {
                double calX = -1;
                double calY = -1;

                for (double beta = 0; beta < 45; beta++)
                {
                    calX = CalculateX( alpha, beta );
                    if ( InRange( calX, x, TOLERANCE ) )
                    {
                        calY = CalculateY( alpha, beta );
               //         Console.WriteLine($" Try x: { calX }, y: { calY }, Destination: { x }, { y } " );
                        if ( InRange( calY, y, TOLERANCE ) )
                        {
                            // Console.WriteLine(" FOUND XY " + calX);
                            // Console.WriteLine(" FOUND calculating " + calX);
                            // Console.WriteLine( $" Aplha: { alpha } Beta: { beta } ");
                        
                            return new MovementProperty( alpha, beta );
                        }
                     }
                }
            }
            Console.WriteLine(" not found ");
            return null;
        }
        static bool InRange( double val, double destination, double tolerance )
        {
            return val > destination - tolerance && val < destination + tolerance;
        }
        static double CalculateX( double alpha, double beta )
        {
            return horizontalLength - B1_LENGTH *  MathHelper.Cos( alpha ) - B2_LENGTH *  MathHelper.Cos ( beta );
        }
        private static double CalculateY(double alpha, double beta)
        {
            return B1_LENGTH *  MathHelper.Sin( alpha ) + B2_LENGTH *  MathHelper.Sin( beta );
        }

        private static double CalculateTetha(double alpha, double beta)
        {
            double lambda =  MathHelper.CosAngle(  MathHelper.Sin ( alpha ) );
            return lambda + beta ;//+ 90;
        }
    }
}
