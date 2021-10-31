using System.IO;
using System.Text.Json;

namespace MovementManager
{
    public class Setting
    {
        private static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public double ArmBaseLength { get; set; }
        public double ArmSecondaryLength { get; set; }
        public byte ArmSecondaryAngleAdjustment { get; set; }
        public double ArmPenDownAngle { get; set; }
        public double Tolerance { get; set; }
        public int DelayBeforeTogglePen { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get;  set; } = 9600;
        public bool SimulationMode{get;set;} = true;
        
        public string Json()
        {
            return JsonSerializer.Serialize<Setting>(this, options);
        }
        public static Setting FromFile(string path)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (StreamReader r = new StreamReader(path))
            {
                while (r.Peek() >= 0)
                {
                    sb.Append(r.ReadLine());
                }
            }
            return FromJson(sb.ToString());

        }
        public static Setting FromJson(string json)
        {
            return (Setting)JsonSerializer.Deserialize<Setting>(json, options);
        }
    }
}