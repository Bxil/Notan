using Notan.Reflection;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity
    {
        public byte Value;

        void IEntity.Deserialize<T>(T deserializer)
        {
            Value = deserializer.Entry(nameof(Value)).ReadByte();
        }

        void IEntity.Serialize<T>(T serializer)
        {
            serializer.Entry(nameof(Value)).Write(Value);
        }
    }
}
