using System;

namespace Notan
{
    [Flags]
    public enum StorageFlags
    {
        None = 0,
        /// <summary>
        /// Authenticated clients can create this entity.
        /// </summary>
        AuthenticatedAuthority = 1,
        /// <summary>
        /// Any client can create this entity, even if it is unauthenticated.
        /// </summary>
        UnauthenticatedAuthority = 2,
        /// <summary>
        /// This entity is not serialized when serializing the whole world.
        /// </summary>
        Impermanent = 4,
        /// <summary>
        /// Clients will explicitly have to forget entities no longer observed.
        /// </summary>
        Linger = 8,
    }

    internal static class StorageFlagsExtensions
    {
        public static bool Has(this StorageFlags flags, StorageFlags flag) => (flags & flag) != StorageFlags.None;
    }
}