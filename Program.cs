using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using arduino_client_hand_writer.Serial;
using MovementManager.Helper;
using MovementManager.InputProcess;
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
        static double sin45 = MathHelper.Sin(45);

        static double verticalLength, horizontalLength;

        static Motor baseMotorComponent, secondaryMotorComponent, penMotorComponent;
        static Led ledComponent;
        const string settingFile = "Resources/settings.json";
        static Setting setting;
        static int[][] imageCode;

        static void Main(string[] args)
        {
            ConfigureLogger();

            setting = Setting.FromFile(settingFile);

            ConfigureBounds();

            int w = (int)(maxX - minX);
            int h = (int)(maxY - minY);
            Console.WriteLine($"W: {w}, H: {h}");
            Bitmap image = ImageLoader.LoadImage("Input/SampleFont.bmp", w, h );
            imageCode = ImageLoader.GetBlackAndWhiteImageCode(image);

            IClient client = setting.SimulationMode ? new MockClient(): SerialClient.Create(setting.PortName, setting.BaudRate, false);
            IService service = new ServiceImpl(client);
            service.Connect();

            InitComponent(service);
            
            Task.Run(() =>
            {
               
                if (setting.SimulationMode == false)
                    ResetHardware();

                DrawFromImageCode( imageCode );

                if (setting.SimulationMode == false)
                    ResetHardware();
                
                service.Close();

                Console.WriteLine(" ======== END ========= ");
            });

           
            Console.ReadLine();

            if (service.Connected)
            {
                service.Close();
            }

        }

        private static void ConfigureBounds()
        {
            verticalLength      = setting.ArmBaseLength + setting.ArmSecondaryLength;
            horizontalLength    = setting.ArmBaseLength + setting.ArmSecondaryLength;

            // maximum value
            maxX = horizontalLength - setting.ArmSecondaryLength;
            maxY = (setting.ArmBaseLength * sin45 + setting.ArmBaseLength * sin45);

            // minimum value
            minX = horizontalLength - (setting.ArmBaseLength * cos45 + setting.ArmBaseLength * cos45);
            minY = setting.ArmBaseLength;

        }

        private static void InitComponent(IService service)
        {
            baseMotorComponent = new Motor(HardwarePin.MOTOR_A_PIN, service) { EnableStepMove = true, AngleStep = 10 };
            secondaryMotorComponent = new Motor(HardwarePin.MOTOR_B_PIN, service) { EnableStepMove = true, AngleStep = 10 };
            penMotorComponent = new Motor(HardwarePin.MOTOR_PEN_PIN, service);
            ledComponent = new Led(HardwarePin.DEFAULT_LED, service);
        }

        private static void Draw()
        {
           
            Console.WriteLine($"MAX Horizontal Length: { maxX }, MAX Vertical Length: { maxY } ");

            ICollection<MovementProperty> movementProperties = new LinkedList<MovementProperty>();
            for (double y = minY; y < maxY; y++)
            {
                for (double x = minX; x < maxX; x++)
                {
                    try
                    {
                        MovementProperty prop = GetMovementProperty(x, y);
                        AddIfNotExist(movementProperties, prop);

                        Console.WriteLine($"[x: { x }, y: { y }] alpha: { prop.Alpha }, beta: { prop.Beta }");
                        Console.WriteLine($"[x: { (byte)prop.X }, y: { (byte)prop.Y }]");
                    }
                    catch (Exception e)
                    {
                        //   Console.WriteLine($"[Point Error] {x}, {y}, error: {e.Message}");
                    }
                }
                //  break;
            }

            SaveToFile(movementProperties);
            ExecuteDraw(movementProperties);

            ToggleLedFinishOperation();

        }
        private static void DrawFromImageCode(int[][] imageCode)
        {
           
            Console.WriteLine($"MAX Horizontal Length: { maxX }, MAX Vertical Length: { maxY } ");

            ICollection<MovementProperty> movementProperties = new LinkedList<MovementProperty>();
            for (int x = 0; x < imageCode.Length; x++)
            {
                for (int y = 0; y < imageCode[x].Length; y++)
                {
                    // Console.WriteLine($"imageCode[x][y] : { imageCode[x][y]  }");
                    if (imageCode[x][y] != 1) continue;
                    try
                    {
                        MovementProperty prop = GetMovementProperty(x + minX, maxY - ( y ));
                        AddIfNotExist(movementProperties, prop);

                        Console.WriteLine($"[x: { x }, y: { y }] alpha: { prop.Alpha }, beta: { prop.Beta }");
                        Console.WriteLine($"[x: { (byte)prop.X }, y: { (byte)prop.Y }]");
                    }
                    catch (Exception e)
                    {
                        //   Console.WriteLine($"[Point Error] {x}, {y}, error: {e.Message}");
                    }
                }
                //  break;
            }

            SaveToFile(movementProperties);
            ExecuteDraw(movementProperties);

            ToggleLedFinishOperation();

        }

        private static void AddIfNotExist(ICollection<MovementProperty> movementProperties, MovementProperty prop)
        {
            foreach (MovementProperty propItem in movementProperties)
            {
                if (propItem.AngleEquals(prop))
                {
                    return;
                }
            }
             movementProperties.Add(prop);
        }

        private static void SaveToFile(ICollection<MovementProperty> movementProperties)
        {

            string jsonMovements = JsonHelper.ToJson(movementProperties);
            string jsonSetting = JsonHelper.ToJson(setting);
            string content = $" const calculatedPaths = {jsonMovements};\n " +
                                      $" const appSettings = {jsonSetting};";

            File.WriteAllText($"Output/path_{DateTime.Now:HHmmss}.json", jsonMovements);
            File.WriteAllText($"Output/data.js", content);
        }

        private static void ExecuteDraw(ICollection<MovementProperty> movementProperties)
        {
            foreach (MovementProperty prop in movementProperties)
            {
                MoveArm(prop);
                Thread.Sleep(setting.DelayBeforeTogglePen);
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
            baseMotorComponent.Move((byte)prop.Alpha);
            // secondaryMotorComponent.Move((byte) prop.Theta);
            secondaryMotorComponent.Move((byte)(prop.Beta + prop.Omega));
        }

        private static void TogglePen()
        {
            ToggleLed(true);

            // move down pen
            PenDown();
            PenUp();

            ToggleLed(false);
        }

        private static void PenUp()
        {
            penMotorComponent.Move(0, 500);
        }

        private static void PenDown()
        {
            penMotorComponent.Move((byte)setting.ArmPenDownAngle, 1000);
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
                    if (MathHelper.InRange(calX, x, setting.Tolerance))
                    {
                        double calY = CalculateY(alpha, beta);
                        if (MathHelper.InRange(calY, y, setting.Tolerance))
                        {
                            //     Console.WriteLine($"<!> [{ x }, { y }] Trial => x: { calX }, y: { calY }");
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

        static double CalculateX(double alpha, double beta)
        {
            double baseArmLengthHorizontal = setting.ArmBaseLength * MathHelper.Cos(alpha);
            double secondaryArmLengthHorizontal = setting.ArmSecondaryLength * MathHelper.Cos(beta);

            return horizontalLength - baseArmLengthHorizontal - secondaryArmLengthHorizontal;
        }
        private static double CalculateY(double alpha, double beta)
        {
            double baseArmLengthVertical = setting.ArmBaseLength * MathHelper.Sin(alpha);
            double secondaryArmLengthVertical = setting.ArmSecondaryLength * MathHelper.Sin(beta);

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
            double baseArmPependicular = setting.ArmBaseLength * MathHelper.Tan(alpha);
            double baseArmLengthVertical = setting.ArmBaseLength * MathHelper.Sin(alpha);
            return (byte)MathHelper.SinAngle(baseArmLengthVertical / baseArmPependicular);
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
