using System;
using System.Threading;
using MovementManager.Helper;
using MovementManager.Model;
using MovementManager.Serial;
using serial_communication_client.Commands;

namespace MovementManager
{
    class Program
    {
        const int MOTOR_A_PIN = 9;
        const int MOTOR_B_PIN = 10;
        const int MAX_DEGREE = 180;
        const int B1_LENGTH = 20, B2_LENGTH = 20;
        const double TOLERANCE = 0.5;

        static double verticalLength, horizontalLength;
        static SerialClient client;
        static void Main(string[] args)
        {
            client = SerialClient.Create( "COM7", 9600 );
            client.Connect();

            ResetMotor();
            verticalLength = VerticalLength();
            horizontalLength = HorizontalLength(); 
            double maxX = (horizontalLength - B1_LENGTH);
            for (int x = 0; x < maxX; x++)
            {
                for (int y = B1_LENGTH; y < verticalLength; y++)
                {
                    Move( x, y );
                    Console.WriteLine( $"x: { x }, y: { y }" );
                }
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
                throw new ArgumentException("Point not feasible");
            }
            MoveMotor( prop );
            return prop;
        }

        private static void MoveMotor(MovementProperty prop)
        {
            double alpha = prop.Alpha;
            double tetha = CalculateTetha( alpha, prop.Beta );
            Console.WriteLine( $"Move motor with aplha: { alpha }, tetha: { tetha }");

            CommandMotorPayload cmdAlpha = CommandMotorPayload.NewCommand( MOTOR_A_PIN, (byte) alpha );
            CommandMotorPayload cmdTetha = CommandMotorPayload.NewCommand( MOTOR_B_PIN, (byte) tetha );
            client.Send( cmdAlpha );
            Thread.Sleep( 500 );
            client.Send( cmdTetha );
            Thread.Sleep( 500 );
        }

        static void ResetMotor()
        {
            CommandMotorPayload cmdAlpha = CommandMotorPayload.NewCommand( MOTOR_A_PIN, 0 );
            CommandMotorPayload cmdTetha = CommandMotorPayload.NewCommand( MOTOR_B_PIN, 0 );
            client.Send( cmdAlpha );
            Thread.Sleep( 500 );
            client.Send( cmdTetha );
            Thread.Sleep( 500 );
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
