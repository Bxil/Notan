using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated, Associated = typeof(Associated))]
public partial struct ByteEntity : IEntity<ByteEntity>
{
    [Serialize]
    public byte Value;

    private sealed class Associated : Associated<ByteEntity>
    {
        public override void OnDestroy(Handle<ByteEntity> handle, ref ByteEntity entity) { }

        public override void PostUpdate(Handle<ByteEntity> handle, ref ByteEntity entity) => entity.Value++;

        public override void PreUpdate(Handle<ByteEntity> handle, ref ByteEntity entity) { }
    }
}

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated, Associated = typeof(Associated))]
public partial struct ByteEntityOnDestroy : IEntity<ByteEntityOnDestroy>
{
    [Serialize]
    public byte Value;

    private sealed class Associated : Associated<ByteEntityOnDestroy>
    {
        public override void OnDestroy(Handle<ByteEntityOnDestroy> handle, ref ByteEntityOnDestroy entity) => entity.Value--;

        public override void PostUpdate(Handle<ByteEntityOnDestroy> handle, ref ByteEntityOnDestroy entity) { }

        public override void PreUpdate(Handle<ByteEntityOnDestroy> handle, ref ByteEntityOnDestroy entity) { }
    }
}