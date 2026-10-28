using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PitchReserve.Api.Serialization;

/// <summary>Rejects ambiguous booking timestamps instead of assuming the server's local timezone.</summary>
public sealed class ExplicitOffsetDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || reader.GetString() is not { } text ||
            !Regex.IsMatch(text, @"(?:Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.CultureInvariant) ||
            !reader.TryGetDateTimeOffset(out var value))
            throw new JsonException("Supply an ISO 8601 timestamp with Z or an explicit UTC offset.");
        return value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime());
}
