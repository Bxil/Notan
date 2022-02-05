using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Tests;

[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct ByteEntityOnDestroy : IEntity<ByteEntityOnDestroy>
{
    public byte Value;

    void IEntity<ByteEntityOnDestroy>.Deserialize<T>(Key key, T entry)
    {
        Value = key == nameof(Value) ? entry.GetByte() : throw new Exception();
    }

    void IEntity<ByteEntityOnDestroy>.Serialize<T>(T serializer)
    {
        serializer.ObjectNext(nameof(Value)).Write(Value);
    }

    void IEntity<ByteEntityOnDestroy>.OnDestroy(ServerHandle<ByteEntityOnDestroy> handle)
    {
        Value -= 1;
    }
}


[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct ByteEntityPreUpdate : IEntity<ByteEntityPreUpdate>
{
    public byte Value;

    void IEntity<ByteEntityPreUpdate>.Deserialize<T>(Key key, T entry)
    {
        Value = key == nameof(Value) ? entry.GetByte() : throw new Exception();
    }

    void IEntity<ByteEntityPreUpdate>.Serialize<T>(T serializer)
    {
        serializer.ObjectNext(nameof(Value)).Write(Value);
    }

    void IEntity<ByteEntityPreUpdate>.PreUpdate(Handle<ByteEntityPreUpdate> handle)
    {
        Value += 1;
    }
}

[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct ByteEntityPostUpdate : IEntity<ByteEntityPostUpdate>
{
    public byte Value;

    void IEntity<ByteEntityPostUpdate>.Deserialize<T>(Key key, T entry)
    {
        Value = key == nameof(Value) ? entry.GetByte() : throw new Exception();
    }

    void IEntity<ByteEntityPostUpdate>.Serialize<T>(T serializer)
    {
        serializer.ObjectNext(nameof(Value)).Write(Value);
    }

    void IEntity<ByteEntityPostUpdate>.PostUpdate(Handle<ByteEntityPostUpdate> handle)
    {
        Value += 1;
    }
}