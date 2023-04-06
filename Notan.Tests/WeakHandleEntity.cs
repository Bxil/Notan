using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
partial struct WeakHandleEntity : IEntity<WeakHandleEntity>
{
    [Serialize(nameof(Value))]
    [HandleIs(typeof(ByteEntity), true)]
    private Handle handle;
}