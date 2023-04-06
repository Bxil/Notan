using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
public partial struct ByteEntity : IEntity<ByteEntity>
{
    [Serialize]
    public byte Value;
}