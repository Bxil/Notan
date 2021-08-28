using System;
using System.Reflection;

namespace Notan.Reflection
{
    public static class WorldExtensions
    {
        /// <summary>
        /// Adds a Storage for every IEntity implementor in the given assembly.
        /// </summary>
        public static void AddStorages(this World world, Assembly assembly)
        {
            var arr = new object?[1];
            foreach (var type in assembly.GetTypes())
            {
                arr[0] = type.GetCustomAttribute<StorageOptionsAttribute>();
                if (typeof(IEntity).IsAssignableFrom(type))
                {
                    world.GetType().GetMethod(nameof(world.AddStorage))!.MakeGenericMethod(type).Invoke(world, arr);
                }
                else if (arr[0] != null)
                {
                    throw new Exception($"{type} has {nameof(StorageOptionsAttribute)} without implementing {nameof(IEntity)}.");
                }
            }
        }
    }
}
