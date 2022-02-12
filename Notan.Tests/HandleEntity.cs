using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct HandleEntity : IEntity<HandleEntity>
{
    [Serialize]
    public Maybe<ByteEntityOnDestroy> Value;
}
