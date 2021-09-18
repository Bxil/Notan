using Notan.Reflection;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct HandleEntity : IEntity<HandleEntity>
    {
        public Handle Value;

        void IEntity<HandleEntity>.Deserialize<TEntry, TArray, TObject>(string key, TEntry entry)
        {
            Value = key switch
            {
                nameof(Value) => entry.GetHandle<ByteEntity>(),
                _ => throw new Exception(),
            };
        }

        void IEntity<HandleEntity>.Serialize<TEntry, TArray, TObject>(TObject serializer)
        {
            serializer.Next(nameof(Value)).Write(Value);
        }
    }
}
