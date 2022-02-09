using Notan.Reflection;
using Notan.Serialization;
using System;

namespace Notan.Tests;

[AutoSerialize]
[StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
partial struct HandleEntity : IEntity<HandleEntity>
{
    [AutoSerialize]
    public Handle<ByteEntityOnDestroy> Value;
}
