using Notan.Reflection;
using Notan.Serialization;

namespace Notan.Tests;

[AutoSerialized]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct HandleEntity : IEntity<HandleEntity>
{
    [AutoSerialize]
    public Maybe<ByteEntityOnDestroy> Value;
}
