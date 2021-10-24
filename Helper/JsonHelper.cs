using System.Text.Json;

namespace MovementManager.Helper
{
    public static class JsonHelper
    {
        static JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
        public static string ToJson<T>(T o)
        {
            return JsonSerializer.Serialize<T>(o, options);
        }
    }
}