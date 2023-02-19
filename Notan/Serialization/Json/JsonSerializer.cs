using System;
using System.Text.Json;

namespace Notan.Serialization.Json;

public readonly struct JsonSerializer : ISerializer<JsonSerializer>
{
    private readonly Utf8JsonWriter writer;

    public JsonSerializer(Utf8JsonWriter writer) => this.writer = writer;

    public void Serialize(byte value) => writer.WriteNumberValue(value);

    public void Serialize(sbyte value) => writer.WriteNumberValue(value);

    public void Serialize(string value) => writer.WriteStringValue(value);

    public void Serialize(bool value) => writer.WriteBooleanValue(value);

    public void Serialize(short value) => writer.WriteNumberValue(value);

    public void Serialize(ushort value) => writer.WriteNumberValue(value);

    public void Serialize(int value) => writer.WriteNumberValue(value);

    public void Serialize(uint value) => writer.WriteNumberValue(value);

    public void Serialize(long value) => writer.WriteNumberValue(value);

    public void Serialize(ulong value) => writer.WriteNumberValue(value);

    public void Serialize(float value) => writer.WriteNumberValue(value);

    public void Serialize(double value) => writer.WriteNumberValue(value);

    public void ArrayBegin() => writer.WriteStartArray();

    public JsonSerializer ArrayNext()
    {
        if (writer.BytesPending >= Environment.SystemPageSize)
        {
            writer.Flush();
        }
        return this;
    }

    public void ArrayEnd() => writer.WriteEndArray();

    public void ObjectBegin() => writer.WriteStartObject();

    public JsonSerializer ObjectNext(string key)
    {
        if (writer.BytesPending >= Environment.SystemPageSize)
        {
            writer.Flush();
        }
        writer.WritePropertyName(key);
        return this;
    }

    public void ObjectEnd() => writer.WriteEndObject();
}
