using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Tests
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    partial struct ByteEntity : IEntity<ByteEntity>
    {
        public byte Value;

        void IEntity<ByteEntity>.Deserialize<T>(Key key, T entry)
        {
            Value = key == nameof(Value) ? entry.GetByte() : throw new Exception();
        }

        void IEntity<ByteEntity>.Serialize<T>(T serializer)
        {
            serializer.ObjectNext(nameof(Value)).Write(Value);
        }

        void IEntity<ByteEntity>.OnDestroy(ServerHandle<ByteEntity> handle)
        {
            Value -= 1;
        }
    }
}
