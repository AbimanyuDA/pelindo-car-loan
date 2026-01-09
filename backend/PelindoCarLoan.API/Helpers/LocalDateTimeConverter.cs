using System.Text.Json;
using System.Text.Json.Serialization;

namespace PelindoCarLoan.API.Helpers
{
    /// <summary>
    /// Custom JSON converter to handle DateTime without UTC conversion
    /// Ensures DateTime values are serialized/deserialized in their original timezone
    /// </summary>
    public class LocalDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
            {
                return DateTime.MinValue;
            }

            // Parse DateTime and always treat as Unspecified (no timezone conversion)
            if (DateTime.TryParse(dateString, out var dateTime))
            {
                // Force to Unspecified to prevent any timezone conversions
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            }

            throw new JsonException($"Unable to parse '{dateString}' as DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Write DateTime as Unspecified to prevent UTC conversion
            var unspecifiedDateTime = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            
            // Format without 'Z' suffix to indicate it's not UTC
            writer.WriteStringValue(unspecifiedDateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}
