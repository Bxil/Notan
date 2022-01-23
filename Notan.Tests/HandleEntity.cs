using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Tests;

[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct HandleEntity : IEntity<HandleEntity>
{
    public Handle Value;

    void IEntity<HandleEntity>.Deserialize<T>(Key key, T deser)
    {
        Value = key == nameof(Value) ? deser.GetHandle().As<ByteEntity>() : throw new Exception();
    }

    void IEntity<HandleEntity>.Serialize<T>(T serializer)
    {
        serializer.ObjectNext(nameof(Value)).Write(Value);
    }
}
