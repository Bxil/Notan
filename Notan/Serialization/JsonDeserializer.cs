using System.Text.Json;

namespace Notan.Serialization
{
    public struct JsonDeserializer : IDeserializer<JsonDeserializer>
    {
        public World World { get; set; }

        private readonly JsonElement element;
        private JsonElement.ArrayEnumerator arrayEnumerator;

        public JsonDeserializer(World world, JsonElement element)
        {
            World = world;
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
            return new JsonDeserializer(World, arrayEnumerator.Current);
        }

        public JsonDeserializer GetEntry(string name) => new(World, element.GetProperty(name));

        public bool ReadBool() => element.GetBoolean();

        public byte ReadByte() => element.GetByte();

        public short ReadInt16() => element.GetInt16();

        public int ReadInt32() => element.GetInt32();

        public long ReadInt64() => element.GetInt64();

        public string ReadString() => element.GetString()!;

        public float ReadSingle() => element.GetSingle();

        public double ReadDouble() => element.GetDouble();
    }
}
