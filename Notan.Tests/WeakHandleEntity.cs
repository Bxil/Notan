using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
partial struct WeakHandleEntity : IEntity<WeakHandleEntity>
{
    [Serialize]
    [HandleIs(typeof(ByteEntityOnDestroy))]
    public Handle Value;
}