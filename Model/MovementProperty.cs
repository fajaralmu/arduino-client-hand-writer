using System.Globalization;

namespace MovementManager.Model
{
    public class MovementProperty
    {
        public byte Alpha { get; }
        public byte Beta { get; }
        public double X { get; }
        public double Y { get; }

        public string XString => X.ToString("N", setPrecision);
        public string YString => Y.ToString("N", setPrecision);

        static NumberFormatInfo setPrecision = new NumberFormatInfo()
        {
            NumberDecimalDigits = 2
        };

        public MovementProperty(double x, double y, double alpha, double beta)
        {
            Alpha = (byte)alpha;
            Beta = (byte)beta;
            X = x;
            Y = y;
        }
    }
}