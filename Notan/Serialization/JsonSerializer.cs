using System.Text.Json;

namespace Notan.Serialization
{
    public struct JsonSerializer : ISerializer<JsonSerializer>
    {
        private readonly Utf8JsonWriter writer;

        public JsonSerializer(Utf8JsonWriter writer) => this.writer = writer;

        public void BeginArray(int length) => writer.WriteStartArray();

        public void EndArray() => writer.WriteEndArray();

        public void BeginObject() => writer.WriteStartObject();

        public void EndObject() => writer.WriteEndObject();

        public JsonSerializer Entry(string name)
        {
            writer.WritePropertyName(name);
            return this;
        }

        public void Write(byte value) => writer.WriteNumberValue(value);

        public void Write(string value) => writer.WriteStringValue(value);

        public void Write(bool value) => writer.WriteBooleanValue(value);

        public void Write(short value) => writer.WriteNumberValue(value);

        public void Write(int value) => writer.WriteNumberValue(value);

        public void Write(long value) => writer.WriteNumberValue(value);

        public void Write(float value) => writer.WriteNumberValue(value);

        public void Write(double value) => writer.WriteNumberValue(value);
    }
}
