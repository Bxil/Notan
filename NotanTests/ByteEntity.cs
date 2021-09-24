using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity<ByteEntity>
    {
        public byte Value;

        void IEntity<ByteEntity>.Deserialize<T>(Key key, T entry)
        {
            if (key == nameof(Value))
            {
                Value = entry.GetByte();
            }
            else
            {
                throw new Exception();
            }
        }

        void IEntity<ByteEntity>.Serialize<T>(T serializer)
        {
            serializer.ObjectNext(nameof(Value)).Write(Value);
        }

        void IEntity<ByteEntity>.OnDestroy(StrongHandle<ByteEntity> handle)
        {
            Value -= 1;
        }
    }
}
