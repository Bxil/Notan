using Notan.Reflection;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity<ByteEntity>
    {
        public byte Value;

        void IEntity<ByteEntity>.Deserialize<T>(T deserializer)
        {
            Value = deserializer.Entry(nameof(Value)).ReadByte();
        }

        void IEntity<ByteEntity>.Serialize<T>(T serializer)
        {
            serializer.Entry(nameof(Value)).Write(Value);
        }
    }
}
