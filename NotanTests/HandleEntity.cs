using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct HandleEntity : IEntity<HandleEntity>
    {
        public Handle Value;

        void IEntity<HandleEntity>.Deserialize<TEntry, TArray, TObject>(Key key, TEntry entry)
        {
            if (key == nameof(Value))
            {
                Value = entry.GetHandle<ByteEntity>();
            }
            else
            {
                throw new Exception();
            }
        }

        void IEntity<HandleEntity>.Serialize<TEntry, TArray, TObject>(TObject serializer)
        {
            serializer.Next(nameof(Value)).Write(Value);
        }
    }
}
