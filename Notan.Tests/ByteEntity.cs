using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityOnDestroy : IEntity<ByteEntityOnDestroy>
{
    public byte Value;

    void IEntity<ByteEntityOnDestroy>.OnDestroy()
    {
        Value -= 1;
    }

    void ISerializable.Serialize<TSer>(TSer serializer)
    {
        serializer.Write(Value);
    }

    void ISerializable.Deserialize<TDeser>(TDeser deserializer)
    {
        Value = deserializer.GetByte();
    }
}

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityPreUpdate : IEntity<ByteEntityPreUpdate>
{
    [Serialize]
    public byte Value;

    void IEntity<ByteEntityPreUpdate>.PreUpdate(Handle<ByteEntityPreUpdate> handle)
    {
        Value += 1;
    }
}

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityPostUpdate : IEntity<ByteEntityPostUpdate>
{
    [Serialize]
    public byte Value;

    void IEntity<ByteEntityPostUpdate>.PostUpdate(Handle<ByteEntityPostUpdate> handle)
    {
        Value += 1;
    }
}