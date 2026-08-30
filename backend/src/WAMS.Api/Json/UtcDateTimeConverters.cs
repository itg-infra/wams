namespace WAMS.Api.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Coerce incoming DateTimes to Kind=Utc (Postgres/Npgsql requirement for timestamptz columns).
/// Offset-less client input deserializes as Kind=Unspecified, breaking EF SaveChanges.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        EnsureUtcKind(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);

    private static DateTime EnsureUtcKind(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : EnsureUtcKind(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }

    private static DateTime EnsureUtcKind(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
