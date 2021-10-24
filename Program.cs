using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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

        static double maxX;
        static double maxY;
        static double minX = 0;
        static double minY;
        static double cos45 = MathHelper.Cos(45);

        static double verticalLength, horizontalLength;

        static Motor baseMotorComponent, secondaryMotorComponent, penMotorComponent;
        static Led ledComponent;


        static Setting setting;

        static void Main(string[] args)
        {
            ConfigureLogger();
            
            setting = Setting.FromFile("Resources/settings.json");

            IClient client = SerialClient.Create(setting.PortName, setting.BaudRate, false);
            client = new MockClient();

            IService service = new ServiceImpl(client);
            service.Connect();

            InitComponent( service );

          //  ResetHardware();

            Draw();

            // ResetHardware();
            
            service.Close();

            Console.WriteLine(" ======== END ========= ");
            Console.ReadLine();

        }

        private static void InitComponent(IService service)
        {
            baseMotorComponent      = new Motor(HardwarePin.MOTOR_A_PIN, service) { EnableStepMove = true, AngleStep = 10 };
            secondaryMotorComponent = new Motor(HardwarePin.MOTOR_B_PIN, service) { EnableStepMove = true, AngleStep = 10 };
            penMotorComponent       = new Motor(HardwarePin.MOTOR_PEN_PIN, service);
            ledComponent            = new Led(HardwarePin.DEFAULT_LED, service);
        }

        private static void Draw()
        {
            verticalLength = CalculateVerticalLength();
            horizontalLength = CalculateHorizontalLength();

            maxX = horizontalLength;
            maxY = verticalLength;

            minY = setting.ArmBaseLength;

            Console.WriteLine($"MAX Horizontal Length: { maxX }, MAX Vertical Length: { maxY } ");

            ICollection<MovementProperty> movementProperties = new LinkedList<MovementProperty>();
            for (double y = minY; y < maxY; y++)
            {
                for (double x = minX; x < maxX; x++)
                {
                    try
                    {
                        MovementProperty prop = GetMovementProperty(x, y);
                        movementProperties.Add(prop);
                        Console.WriteLine($"[{ x }, y: { y }] alpha: { prop.Alpha }, beta: { prop.Beta }");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[Point Error] {x}, {y}, error: {e.Message}");
                    }
                }
                //  break;
            }

            SaveToFile(movementProperties);
      //      ExecuteDraw(movementProperties);

            ToggleLedFinishOperation();

        }

        private static void SaveToFile(ICollection<MovementProperty> movementProperties)
        {
           string json = JsonSerializer.Serialize<ICollection<MovementProperty>>(movementProperties, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
            File.WriteAllText($"Output/path_{DateTime.Now:HHmmss}.json", json);
            File.WriteAllText($"Output/data.js", "const calculatedPaths = " + json );
        }

        private static void ExecuteDraw(ICollection<MovementProperty> movementProperties)
        {
            foreach (MovementProperty prop in movementProperties)
            {
                MoveArm(prop);
                Thread.Sleep( setting.DelayBeforeTogglePen );
                TogglePen();
            }
        }

        private static void ToggleLedFinishOperation()
        {
            int delay = 5;
            for (var i = 0; i < 10; i++)
            {
                ToggleLed(true, delay);
                ToggleLed(false, delay / 2);
            }

        }

        static bool ValidateX(double x)
        {
            bool result = x >= minX && x <= maxX;
            //   Console.WriteLine($"X = { x } min: { 0 } max: { maxX } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid X: { x }. Range = {minX} - {maxX}");
            }

            return result;
        }
        static bool ValidateY(double y)
        {
            bool result = y >= minY && y <= maxY;
            //    Console.WriteLine($"Y = { y } min: { B1_LENGTH } max: { verticalLength } -valid = { result }");
            if (!result)
            {
                throw new ArgumentException($"Invalid Y: { y }. Range: {minY} - {maxY}");
            }

            return result;
        }

        static double CalculateVerticalLength()
        {
            return ( setting.ArmBaseLength + setting.ArmSecondaryLength ) * cos45;
        }
        static double CalculateHorizontalLength()
        {
            return setting.ArmSecondaryLength;// * cos45 + setting.ArmSecondaryLength * cos45;
        }

        ///////////////////////////// Movement Model //////////////////////////////

        static MovementProperty GetMovementProperty(double x, double y)
        {
            ValidateX(x);
            ValidateY(y);
            MovementProperty prop = CalculateMovement(x, y);
            if (null == prop)
            {
                throw new ArgumentException($"X,Y valid. but not feasible: {x}, {y}");
            }

            return prop;
        }

        private static void MoveArm(MovementProperty prop)
        {
            Console.WriteLine($"Move motor ({prop.XString}, {prop.YString})");
            
            // Move arms
            baseMotorComponent.Move((byte) prop.Alpha);
            // secondaryMotorComponent.Move((byte) prop.Theta);
            secondaryMotorComponent.Move((byte) prop.Omega);
        }

        private static void TogglePen()
        {
            ToggleLed(true);

            // move down pen
            penMotorComponent.Move((byte) setting.ArmPenDownAngle, 1000);
            penMotorComponent.Move(0, 500);

            ToggleLed(false);
        }

        private static void ToggleLed(bool on, int waitDuration = 0)
        {
            ledComponent.Toggle(on, waitDuration);
        }

        static void ResetHardware()
        {
            Console.WriteLine(" ======= Start Reset Hardware ======= ");
            ToggleLed(true, 1000);

            // Reset ARM
            baseMotorComponent.Move(0, 1000);
            secondaryMotorComponent.Move(0, 1000);

            // Reset PEN 
            penMotorComponent.Move(0, 1000);

            ToggleLed(false, 1000);
            Console.WriteLine(" ======= End Reset Hardware ======= ");
        }

        static MovementProperty CalculateMovement(double x, double y)
        {

            for (double alpha = 0; alpha < 90; alpha++)
            {

                for (double beta = 0; beta < 45; beta++)
                {
                    double calX = CalculateX(alpha, beta);
                    if (InRange(calX, x, setting.Tolerance))
                    {
                        double calY = CalculateY(alpha, beta);
                        //   Console.WriteLine($"[{ x }, { y }] Trial => x: { calX }, y: { calY }" );
                        if (InRange(calY, y, setting.Tolerance))
                        {
                            // Console.WriteLine(" FOUND XY " + calX);
                            // Console.WriteLine(" FOUND calculating " + calX);
                            // Console.WriteLine( $" Aplha: { alpha } Beta: { beta } ");
                            Console.WriteLine($"<!> [{ x }, { y }] Trial => x: { calX }, y: { calY }");
                            double theta = CalculateTetha(alpha, beta);
                            double omega = CalculateOmega(alpha);
                            return new MovementProperty(calX, calY, alpha, beta, theta, omega);
                        }
                    }
                }
            }
            // Console.WriteLine($" not found : {x}, {y}");
            return null;
        }
        static bool InRange(double val, double destination, double tolerance)
        {
            return val > destination - tolerance && val < destination + tolerance;
        }
        static double CalculateX(double alpha, double beta)
        {
            double baseArmLengthHorizontal      = setting.ArmBaseLength * MathHelper.Cos(alpha);
            double secondaryArmLengthHorizontal = setting.ArmSecondaryAngleAdjustment * MathHelper.Cos(beta);
            
            return horizontalLength - baseArmLengthHorizontal - secondaryArmLengthHorizontal;
        }
        private static double CalculateY(double alpha, double beta)
        {
            double baseArmLengthVertical      = setting.ArmBaseLength * MathHelper.Sin(alpha);
            double secondaryArmLengthVertical =  setting.ArmSecondaryAngleAdjustment * MathHelper.Sin(beta);

            return baseArmLengthVertical + secondaryArmLengthVertical;
        }

        private static byte CalculateTetha(double alpha, double beta)
        {
            double lambda = MathHelper.CosAngle(MathHelper.Sin(alpha));
            return (byte)(lambda + beta);//+ 90;
        }
        // relative angle from base arm latest position against x axis
        private static byte CalculateOmega(double alpha)
        {
            double p = setting.ArmBaseLength / MathHelper.Tan( alpha );
            double horArmBaseLength  = setting.ArmBaseLength * MathHelper.Sin( alpha );
            return (byte) MathHelper.SinAngle( horArmBaseLength / p );
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
