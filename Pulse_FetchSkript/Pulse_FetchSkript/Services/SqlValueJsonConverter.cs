using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse_FetchSkript.Services;

/// <summary>
/// Serialisiert beliebige Werte aus SqlDataReader (DateTime, decimal, byte[], Guid, …)
/// in JSON. Strings bleiben Strings; DateTime wird ISO-8601; byte[] wird Base64.
/// </summary>
public sealed class SqlValueJsonConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True: return true;
                case JsonTokenType.False: return false;
                case JsonTokenType.Null: return null;
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out var l) ? l : reader.GetDouble();
                case JsonTokenType.String:
                    return reader.GetString();
                default:
                    var doc = JsonDocument.ParseValue(ref reader);
                    var clone = doc.RootElement.Clone();
                    doc.Dispose();
                    return clone;
            }
        }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case byte bt:
                writer.WriteNumberValue(bt);
                break;
            case short sh:
                writer.WriteNumberValue(sh);
                break;
            case ushort ush:
                writer.WriteNumberValue(ush);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case uint ui:
                writer.WriteNumberValue(ui);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case ulong ul:
                writer.WriteNumberValue(ul);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case decimal dec:
                writer.WriteNumberValue(dec);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt.ToUniversalTime().ToString("O"));
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto.ToUniversalTime().ToString("O"));
                break;
            case DateOnly dateOnly:
                writer.WriteStringValue(dateOnly.ToString("O"));
                break;
            case TimeOnly timeOnly:
                writer.WriteStringValue(timeOnly.ToString("O"));
                break;
            case Guid g:
                writer.WriteStringValue(g);
                break;
            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }
}