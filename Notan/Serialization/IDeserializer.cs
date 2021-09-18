using System;
using System.Diagnostics;

namespace Notan.Serialization
{
    public interface IDeserializerEntry<TEntry, TArray, TObject>
        where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
        where TArray : IDeserializerArray<TEntry, TArray, TObject>
        where TObject : IDeserializerObject<TEntry, TArray, TObject>
    {
        public World World { get; }

        bool GetBool();
        byte GetByte();
        short GetInt16();
        int GetInt32();
        long GetInt64();
        float GetSingle();
        double GetDouble();
        string GetString();

        TArray GetArray();
        TObject GetObject();

        public Handle GetHandle<TEntity>() where TEntity : struct, IEntity<TEntity>
        {
            var array = GetArray();
            return new Handle(World.GetStorageBase<TEntity>(), array.Next().GetInt32(), array.Next().GetInt32());
        }
    }

    public interface IDeserializerArray<TEntry, TArray, TObject>
        where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
        where TArray : IDeserializerArray<TEntry, TArray, TObject>
        where TObject : IDeserializerObject<TEntry, TArray, TObject>
    {
        bool Next(out TEntry entry);
        public TEntry Next()
        {
            if (Next(out var entry))
            {
                return entry;
            }
            throw new Exception("Array had no more elements.");
        }
    }

    public interface IDeserializerObject<TEntry, TArray, TObject>
        where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
        where TArray : IDeserializerArray<TEntry, TArray, TObject>
        where TObject : IDeserializerObject<TEntry, TArray, TObject>
    {
        bool Next(out string key, out TEntry value);
        public TEntry Next(out string key)
        {
            if (Next(out key, out var entry))
            {
                return entry;
            }
            throw new Exception("Array had no more elements.");
        }
    }
}
