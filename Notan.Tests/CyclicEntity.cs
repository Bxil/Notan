using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct CyclicEntity : IEntity<CyclicEntity>
{
    [Serialize]
    [HandleIs(typeof(CyclicEntity))]
    public Handle Self;

    void IEntity<CyclicEntity>.PostUpdate(Handle<CyclicEntity> handle)
    {
        Self = handle;
    }

    void IEntity<CyclicEntity>.OnDestroy()
    {
        Self.Server<CyclicEntity>().Destroy();
    }
}