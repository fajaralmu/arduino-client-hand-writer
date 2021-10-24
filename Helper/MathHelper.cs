using System;

namespace MovementManager.Helper
{
    public static class MathHelper
    {
        public static double CosAngle( double cos )
        {
            double rad = Math.Acos( cos );
            return Angle( rad );
        }

        public static double SinAngle( double sin )
        {
            double rad = Math.Asin( sin );
            return Angle( rad );
        }

        public static double Angle(double rad)
        {
            return rad * 180 / Math.PI;
        }

        public static double Rad( double angle )
        {
            return angle * Math.PI / 180;
        }
        public static double Cos( double angle )
        {
            return Math.Cos( Rad( angle ) );
        }
        public static double Sin( double angle )
        {
            return Math.Sin( Rad( angle ) );
        }

        internal static double Tan(double angle )
        {
            return Math.Tan( Rad( angle ) );
        }

        internal static bool InRange(double val, double destination, double tolerance)
        {
            return val > destination - tolerance && val < destination + tolerance;
        }
    }
}