namespace MovementManager.Model
{
    public class MovementProperty
    {
        public byte Alpha {get;}
        public byte Beta {get;}

        public MovementProperty( double alpha, double beta )
        {
            Alpha = (byte) alpha;
            Beta = (byte) beta;
        }
    }
}