using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[AutoSerialize]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct HandleEntity : IEntity<HandleEntity>
{
    [AutoSerialize]
    public Maybe<ByteEntityOnDestroy> Value;
}
