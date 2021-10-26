using System;

namespace Notan.Reflection
{
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class StorageOptionsAttribute : Attribute
    {
        public readonly StorageFlags Flags;

        public StorageOptionsAttribute(StorageFlags flags = StorageFlags.None)
        {
            Flags = flags;
        }
    }
}
