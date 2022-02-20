using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
partial struct WeakHandleEntity : IEntity<WeakHandleEntity>
{
    [Serialize(nameof(Value))]
    [HandleIs(typeof(ByteEntityOnDestroy), true)]
    private Handle handle;
}