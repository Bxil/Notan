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
            bool success = array.NextEntry(out var indexEntry);
            Debug.Assert(success);
            int index = indexEntry.GetInt32();
            success = array.NextEntry(out var genEntry);
            Debug.Assert(success);
            int gen = genEntry.GetInt32();
            Debug.Assert(!array.NextEntry(out _));
            return new Handle(World.GetStorageBase<TEntity>(), index, gen);
        }
    }

    public interface IDeserializerArray<TEntry, TArray, TObject>
        where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
        where TArray : IDeserializerArray<TEntry, TArray, TObject>
        where TObject : IDeserializerObject<TEntry, TArray, TObject>
    {
        bool NextEntry(out TEntry entry);
    }

    public interface IDeserializerObject<TEntry, TArray, TObject>
        where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
        where TArray : IDeserializerArray<TEntry, TArray, TObject>
        where TObject : IDeserializerObject<TEntry, TArray, TObject>
    {
        bool NextEntry(out string key, out TEntry value);
    }
}
