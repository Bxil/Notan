using System.Text.Json;

namespace Notan.Serialization
{
    public struct JsonSerializerEntry : ISerializerEntry<JsonSerializerEntry, JsonSerializerArray, JsonSerializerObject>
    {
        private readonly Utf8JsonWriter writer;

        public JsonSerializerEntry(Utf8JsonWriter writer) => this.writer = writer;

        public void Write(byte value) => writer.WriteNumberValue(value);

        public void Write(string value) => writer.WriteStringValue(value);

        public void Write(bool value) => writer.WriteBooleanValue(value);

        public void Write(short value) => writer.WriteNumberValue(value);

        public void Write(int value) => writer.WriteNumberValue(value);

        public void Write(long value) => writer.WriteNumberValue(value);

        public void Write(float value) => writer.WriteNumberValue(value);

        public void Write(double value) => writer.WriteNumberValue(value);

        public JsonSerializerArray WriteArray() => new(writer);

        public JsonSerializerObject WriteObject() => new(writer);
    }

    public struct JsonSerializerArray : ISerializerArray<JsonSerializerEntry, JsonSerializerArray, JsonSerializerObject>
    {
        private readonly Utf8JsonWriter writer;

        public JsonSerializerArray(Utf8JsonWriter writer)
        {
            this.writer = writer;
            writer.WriteStartArray();
        }

        public JsonSerializerEntry Next() => new(writer);

        public void End() => writer.WriteEndArray();
    }

    public struct JsonSerializerObject : ISerializerObject<JsonSerializerEntry, JsonSerializerArray, JsonSerializerObject>
    {
        private readonly Utf8JsonWriter writer;

        public JsonSerializerObject(Utf8JsonWriter writer)
        {
            this.writer = writer;
            writer.WriteStartObject();
        }

        public JsonSerializerEntry Next(string key)
        {
            writer.WritePropertyName(key);
            return new(writer);
        }

        public void End() => writer.WriteEndObject();
    }
}
