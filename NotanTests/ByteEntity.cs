using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Testing
{
    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    struct ByteEntity : IEntity<ByteEntity>
    {
        public byte Value;

        void IEntity<ByteEntity>.Deserialize<TEntry, TArray, TObject>(KeyComparison key, TEntry entry)
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

        void IEntity<ByteEntity>.Serialize<TEntry, TArray, TObject>(TObject serializer)
        {
            serializer.Next(nameof(Value)).Write(Value);
        }
    }
}
