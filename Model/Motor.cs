using System;
using System.Threading;
using MovementManager.Service;
using serial_communication_client;

namespace MovementManager.Model
{
    public class Motor : Component
    {
        public bool EnableStepMove { get; set; }
        public int DelayPerStep { get; set; } = 5;
        public int AngleStep { 
            get => _angleStep;
            set
            {
                if ( value <= 0 )
                {
                    throw new ArgumentOutOfRangeException("AngleStep");
                }
                _angleStep = value;
            } 
        }

        private int _angleStep = 5;
        private int _lastAngle;
        public Motor(HardwarePin pin, IService service) : base(pin, service)
        {
            _lastAngle = _service.ReadMotorAngle(Pin);
        }
        public virtual void Move(byte angle, int waitDuration = 0)
        {
            if (EnableStepMove && Math.Abs(_lastAngle - angle) > AngleStep)
            {
                MoveByStep(angle, waitDuration);
            }
            else
            {
                _service.MoveMotor(Pin, angle, waitDuration);
            }

            // Console.WriteLine("--check servo angle--");

            // int latestAngle = _service.ReadMotorAngle(Pin);

            // while (latestAngle != angle)
            // {
            //     Console.WriteLine("wait latest angle: " + latestAngle);
            //     latestAngle = _service.ReadMotorAngle(Pin);
            // }

            Console.WriteLine($"END MOVE SERVO { Pin } to angle: { angle }, waiting for { waitDuration } ms");

            _lastAngle = angle;
        }

        private void MoveByStep(byte angle, int waitDuration)
        {
            int diff = Math.Abs(_lastAngle - angle);
            int step = diff / AngleStep;
            int increment = angle > _lastAngle ? 1 : -1;

            // move step by step
            for (int i = 1; i <= step; i++)
            {
                int a = _lastAngle + increment * i * AngleStep;
                _service.MoveMotor(Pin, (byte)a, waitDuration);
                Console.WriteLine("MOVE STEP: " + a);

                SleepIfNotNegative( DelayPerStep );
            }

            if (diff % AngleStep > 0)
            {
                // last step
                Console.WriteLine("MOVE STEP (last): " + angle);
                _service.MoveMotor(Pin, angle, waitDuration);

                SleepIfNotNegative( DelayPerStep );
            }
        }

        private void SleepIfNotNegative(int delayPerStep)
        {
            if (delayPerStep > 0)
            {
                Thread.Sleep(delayPerStep);
            }
        }
    }
}