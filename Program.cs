using System;
using System.Diagnostics;
using System.Threading;
using arduino_client_hand_writer.Serial;
using MovementManager.Helper;
using MovementManager.Model;
using MovementManager.Service;
using serial_communication_client;
using serial_communication_client.Serial;

namespace MovementManager
{
    class Program
    {
      
        const double PX_PER_CM = 37.795280352161;
        const double MAX_DEGREE = 180;
        const double B1_LENGTH = 15.5;// 10;//(int)( 15.5 * PX_PER_CM );
        const double B2_LENGTH = 13.5;// 10;//(int)( 13.5 * PX_PER_CM );
        const int PEN_FREE_ANGLE = 30;
        const double TOLERANCE = 0.1;

        static double maxX;
        static double maxY;
        static double minX = 0;
        static double minY = B1_LENGTH;
        static double cos45 = MathHelper.Cos( 45 );

        static double verticalLength, horizontalLength;
        static IService service;
        static void Main(string[] args)
        {
           
            ConfigureLogger();

            IClient client = SerialClient.Create( "COM7", 9600, false );
            // client = new MockClient();

            service = new ServiceImpl( client );
            service.Connect();
            ResetHardware();
            
        //    service.MoveMotor( HardwarePin.MOTOR_PEN_PIN, 90 );
        //    if (false) 
                Draw();
            
           ResetHardware();
          //  service.MoveMotor( HardwarePin.MOTOR_PEN_PIN, 10 );
            Console.WriteLine(" ======== END ========= ");
            Console.ReadLine();

            service.Close();

        }

        private static void Draw()
        {
            verticalLength = CalculateVerticalLength();
            horizontalLength = CalculateHorizontalLength();  
            
            maxX = horizontalLength;
            maxY = verticalLength;

            Console.WriteLine( $"MAX Horizontal Length: { maxX }, MAX Vertical Length: { maxY } ");

            for (double y = minY; y < maxY; y++)
            {
                for (double x = minX; x < maxX; x++)
                {
                    try {
                        MovementProperty prop =  DrawPoint( x, y );
                        Console.WriteLine( $"[{ x }, y: { y }] alpha: { prop.Alpha }, beta: { prop.Beta }" );
                    } catch (Exception)
                    {
                 //      Console.WriteLine($"point not feasible: {x}, {y}");
                    }
                }
              //  break;
            }

            ToggleLedFinishOperation();

        }

        private static void ToggleLedFinishOperation()
        {
            int delay = 5;
            for (var i = 0; i < 10; i++)
            {
                ToggleLed( true, delay );
                ToggleLed( false, delay / 2 );
            }
            
        }

        static bool ValidateX( double x )
        {
            double maxX = (horizontalLength - B1_LENGTH);
            bool result = x >= minX && x <= maxX;
         //   Console.WriteLine($"X = { x } min: { 0 } max: { maxX } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid X: { x }");
            }

            return result;
        }
        static bool ValidateY( double y )
        {
            bool result = y >= minY && y <= maxY;
        //    Console.WriteLine($"Y = { y } min: { B1_LENGTH } max: { verticalLength } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid Y: { y }");
            }

            return result;
        }

        static double CalculateVerticalLength()
        {
            return ( B1_LENGTH + B2_LENGTH ) *  cos45;
        }
        static double CalculateHorizontalLength()
        {
            return B1_LENGTH * cos45 + B2_LENGTH *  cos45;
        }
       
        ///////////////////////////// Movement Model //////////////////////////////

        static MovementProperty DrawPoint( double x, double y )
        {
            ValidateX( x );
            ValidateY( y );
            MovementProperty prop = CalculateMovement( x, y );
            if (null == prop)
            {
                throw new ArgumentException($"Point not feasible: {x}, {y}");
            }
            MoveArm( prop );
            TogglePen();
            return prop;
        }

        private static void MoveArm(MovementProperty prop)
        {
            Console.WriteLine($"Move motor ({prop.XString}, {prop.YString})");
            double alpha = prop.Alpha;
            double tetha = CalculateTetha( alpha, prop.Beta );

            // Move arms
            service.MoveMotor( HardwarePin.MOTOR_A_PIN, (byte) alpha );
            service.MoveMotor( HardwarePin.MOTOR_B_PIN, (byte) tetha );
        }

        private static void TogglePen()
        {
            ToggleLed( true );
            
            HardwarePin pin = HardwarePin.MOTOR_PEN_PIN;
            // move down pen
            service.MoveMotor( pin, 0, 1000 );
            // move up pen
            service.MoveMotor( pin, PEN_FREE_ANGLE, 100 );

            ToggleLed( false );
        }

        private static void ToggleLed( bool on, int waitDuration = 0 )
        {
            service.ToggleLed( HardwarePin.DEFAULT_LED, on, waitDuration );
        }

        static void ResetHardware()
        {
            Console.WriteLine(" ======= Start Reset Hardware ======= ");
            ToggleLed( true, 1000 );

            // Reset ARM
            service.MoveMotor( HardwarePin.MOTOR_A_PIN, 0, 1000 );
            service.MoveMotor( HardwarePin.MOTOR_B_PIN, 0, 1000 );

            // Reset PEN 
            service.MoveMotor( HardwarePin.MOTOR_PEN_PIN, PEN_FREE_ANGLE, 1000 );

            ToggleLed( false, 1000 );
            Console.WriteLine(" ======= End Reset Hardware ======= ");
        }

        static MovementProperty CalculateMovement( double x, double y )
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
                     //   Console.WriteLine($"[{ x }, { y }] Trial => x: { calX }, y: { calY }" );
                        if ( InRange( calY, y, TOLERANCE ) )
                        {
                            // Console.WriteLine(" FOUND XY " + calX);
                            // Console.WriteLine(" FOUND calculating " + calX);
                            // Console.WriteLine( $" Aplha: { alpha } Beta: { beta } ");
                            Console.WriteLine($"<!> [{ x }, { y }] Trial => x: { calX }, y: { calY }" );
                            return new MovementProperty( calX, calY, alpha, beta );
                        }
                     }
                }
            }
            // Console.WriteLine($" not found : {x}, {y}");
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

        private static byte CalculateTetha(double alpha, double beta)
        {
            double lambda =  MathHelper.CosAngle(  MathHelper.Sin ( alpha ) );
            return (byte) ( lambda + beta );//+ 90;
        }

        private static void ConfigureLogger()
        {
            //File
            // _logFile = File.Create($"Logs/RoverSimulation-Log-{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.log");
            // TextWriterTraceListener myTextListener = new TextWriterTraceListener(_logFile);
            // Trace.Listeners.Add(myTextListener);
            
            //Console
            TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
            Trace.Listeners.Add(writer);
        }

    }

    
}
