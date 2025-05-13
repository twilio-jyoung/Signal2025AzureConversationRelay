#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Signal2025AzureConversationRelay.Messages;

public class EscapedJsonConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? json = reader.GetString();
        if (string.IsNullOrEmpty(json))
            return default!;
        return MessageFactory.Deserialize<T>(json)!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        string json = MessageFactory.Serialize(value);
        writer.WriteStringValue(json);
    }
}