using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct HandleEntity : IEntity<HandleEntity>
    {
        public Handle Value;

        void IEntity<HandleEntity>.Deserialize<T>(Key key, T deser)
        {
            if (key == nameof(Value))
            {
                Value = deser.GetHandle<T, ByteEntity>();
            }
            else
            {
                throw new Exception();
            }
        }

        void IEntity<HandleEntity>.Serialize<T>(T serializer)
        {
            serializer.ObjectNext(nameof(Value)).Write(Value);
        }
    }
}
