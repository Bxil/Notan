using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
[StorageOptions(Associated = typeof(Associated))]
public partial struct CyclicEntity : IEntity<CyclicEntity>
{
    [Serialize]
    [HandleIs(typeof(CyclicEntity))]
    public Handle Self;

    private sealed class Associated : Associated<CyclicEntity>
    {
        public override void OnDestroy(Handle<CyclicEntity> handle, ref CyclicEntity entity)
            => entity.Self.Server<CyclicEntity>().Destroy();

        public override void PostUpdate(Handle<CyclicEntity> handle, ref CyclicEntity entity)
            => entity.Self = handle;

        public override void PreUpdate(Handle<CyclicEntity> handle, ref CyclicEntity entity) { }
    }
}