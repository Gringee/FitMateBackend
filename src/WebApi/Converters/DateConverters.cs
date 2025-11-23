using System.Text.Json.Serialization; 
using System.Text.Json;

namespace WebApi.Converters;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string OutputFormat = "HH:mm";
    
    private readonly string[] _inputFormats = { "HH:mm", "HH:mm:ss" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }
        
        return TimeOnly.ParseExact(value, _inputFormats, null, System.Globalization.DateTimeStyles.None);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(OutputFormat));
    }
}

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException($"Cannot convert null or empty string to DateOnly. Expected format: {Format}");
        }
        
        return DateOnly.ParseExact(value, Format, null);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}