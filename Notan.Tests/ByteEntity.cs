using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[AutoSerialized]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityOnDestroy : IEntity<ByteEntityOnDestroy>
{
    [AutoSerialize]
    public byte Value;

    void IEntity<ByteEntityOnDestroy>.OnDestroy(ServerHandle<ByteEntityOnDestroy> handle)
    {
        Value -= 1;
    }
}

[AutoSerialized]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityPreUpdate : IEntity<ByteEntityPreUpdate>
{
    [AutoSerialize]
    public byte Value { get; set; }

    void IEntity<ByteEntityPreUpdate>.PreUpdate(Handle<ByteEntityPreUpdate> handle)
    {
        Value += 1;
    }
}

[AutoSerialized]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityPostUpdate : IEntity<ByteEntityPostUpdate>
{
    [AutoSerialize]
    public byte Value { get; set; }

    void IEntity<ByteEntityPostUpdate>.PostUpdate(Handle<ByteEntityPostUpdate> handle)
    {
        Value += 1;
    }
}