using System.Text.Json;

namespace Notan.Serialization
{
    public struct JsonSerializer : ISerializer
    {
        private readonly Utf8JsonWriter writer;

        public JsonSerializer(Utf8JsonWriter writer) => this.writer = writer;

        public void BeginArray(int length) => writer.WriteStartArray();

        public void EndArray() => writer.WriteEndArray();

        public void BeginObject() => writer.WriteStartObject();

        public void EndObject() => writer.WriteEndObject();

        public void Write(string name, byte value) => writer.WriteNumber(name, value);

        public void Write(string name, string value) => writer.WriteString(name, value);

        public void Write(string name, bool value) => writer.WriteBoolean(name, value);

        public void Write(string name, int value) => writer.WriteNumber(name, value);

        public void Write(string name, long value) => writer.WriteNumber(name, value);

        public void WriteEntry(string name) => writer.WritePropertyName(name);
    }
}
