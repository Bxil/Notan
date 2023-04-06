using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct CyclicEntity : IEntity<CyclicEntity>
{
    [Serialize]
    [HandleIs(typeof(CyclicEntity))]
    public Handle Self;
}