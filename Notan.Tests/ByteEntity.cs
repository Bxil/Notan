using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityOnDestroy : IEntity<ByteEntityOnDestroy>
{
    [Serialize]
    public byte Value;

    void IEntity<ByteEntityOnDestroy>.OnDestroy(ServerHandle<ByteEntityOnDestroy> handle)
    {
        Value -= 1;
    }
}

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntityPreUpdate : IEntity<ByteEntityPreUpdate>
{
    [Serialize]
    public byte Value { get; set; }

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
    public byte Value { get; set; }

    void IEntity<ByteEntityPostUpdate>.PostUpdate(Handle<ByteEntityPostUpdate> handle)
    {
        Value += 1;
    }
}