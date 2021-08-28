using Notan.Reflection;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity
    {
        public Handle Handle { get; set; }

        public byte Value;

        void IEntity.Deserialize<T>(T deserializer)
        {
            Value = deserializer.GetEntry(nameof(Value)).ReadByte();
        }

        void IEntity.Serialize<T>(T serializer)
        {
            serializer.Write(nameof(Value), Value);
        }
    }
}
