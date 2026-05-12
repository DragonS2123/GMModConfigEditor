using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace GMCraftTableEditor.Services;

public static class JsonService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    static JsonService()
    {
        Options.Converters.Add(new FlexibleIntConverter());
        Options.Converters.Add(new FlexibleDoubleConverter());
    }

    public static T Load<T>(string path) where T : new()
    {
        if (!File.Exists(path)) return new T();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
    }

    public static void Save<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(path, json);
    }
}

public sealed class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i)) return i;
            if (reader.TryGetDouble(out var d)) return (int)Math.Round(d);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) return i;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return (int)Math.Round(d);
        }

        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

public sealed class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var d)) return d;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
        }

        return 0.0;
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
