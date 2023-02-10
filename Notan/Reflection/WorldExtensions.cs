﻿using System;
using System.Reflection;

namespace Notan.Reflection;

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
            try //this try is here specifically for the MakeGenericType
            {
                if (!typeof(IEntity<>).MakeGenericType(type).IsAssignableFrom(type))
                {
                    NotanException.Throw($"{type} implements {typeof(IEntity<>)} but not for its own type.");
                }
                _ = world.GetType().GetMethod(nameof(world.AddStorage))!.MakeGenericMethod(type).Invoke(world, arr);
            }
            catch (ArgumentException)
            {
                if (arr[0] != null)
                {
                    NotanException.Throw($"{type} has {nameof(StorageOptionsAttribute)} without implementing {typeof(IEntity<>)}.");
                }
            }
        }
    }
}
