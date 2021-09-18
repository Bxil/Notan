using Notan.Reflection;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity<ByteEntity>
    {
        public byte Value;

        void IEntity<ByteEntity>.Deserialize<TEntry, TArray, TObject>(string key, TEntry entry)
        {
            Value = key switch
            {
                nameof(Value) => entry.GetByte(),
                _ => throw new Exception(),
            };
        }

        void IEntity<ByteEntity>.Serialize<TEntry, TArray, TObject>(TObject serializer)
        {
            serializer.Next(nameof(Value)).Write(Value);
        }
    }
}
