using Notan.Serialization;

namespace Notan.Testing
{
    struct ByteEntity : IEntity
    {
        public Handle Handle { get; set; }

        public byte Value;

        public void Deserialize<T>(T deserializer) where T : IDeserializer<T>
        {
            Value = deserializer.GetEntry(nameof(Value)).ReadByte();
        }

        public void Serialize<T>(T serializer) where T : ISerializer
        {
            serializer.Write(nameof(Value), Value);
        }
    }
}
