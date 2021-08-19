using System.Text.Json;

namespace Notan.Serialization
{
    public struct JsonDeserializer : IDeserializer<JsonDeserializer>
    {
        private readonly JsonElement element;
        private JsonElement.ArrayEnumerator arrayEnumerator;

        public JsonDeserializer(JsonElement element)
        {
            this.element = element;
            arrayEnumerator = default;
        }

        public int BeginArray()
        {
            arrayEnumerator = element.EnumerateArray();
            return element.GetArrayLength();
        }

        public JsonDeserializer NextArrayElement()
        {
            arrayEnumerator.MoveNext();
            return new JsonDeserializer(arrayEnumerator.Current);
        }

        public JsonDeserializer GetEntry(string name) => new(element.GetProperty(name));

        public bool ReadBool() => element.GetBoolean();

        public byte ReadByte() => element.GetByte();

        public int ReadInt32() => element.GetInt32();

        public string ReadString() => element.GetString()!;
    }
}
