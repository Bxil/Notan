using System;

namespace Notan.Reflection;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class StorageOptionsAttribute : Attribute
{
    public ClientAuthority ClientAuthority;
    public bool NoPersistence;
}
