using System;
using System.Numerics;

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
            var arr = GetArray();
            var handle = new Handle(World.GetStorageBase<TEntity>(), arr.Next().GetInt32(), arr.Next().GetInt32());
            arr.Next(out _); //consume the closing bracket
            return handle;
        }

        public Vector3 GetVector3()
        {
            var arr = GetArray();
            var vec = new Vector3(arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle());
            arr.Next(out _); //consume the closing bracket
            return vec;
        }

        public Matrix4x4 GetMatrix4x4()
        {
            var arr = GetArray();
            var mat = new Matrix4x4(
                arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(),
                arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(),
                arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(),
                arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle(), arr.Next().GetSingle()
                );
            arr.Next(out _); //consume the closing bracket
            return mat;
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
        bool Next(out Key key, out TEntry value);
        public TEntry Next(out Key key)
        {
            if (Next(out key, out var entry))
            {
                return entry;
            }
            throw new Exception("Array had no more elements.");
        }
    }
}
