using System;
using System.Diagnostics.CodeAnalysis;

namespace Notan.Serialization;

public interface IDeserializer<T> where T : IDeserializer<T>
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

    void ArrayBegin();
    bool ArrayTryNext();
    T ArrayNext();

    void ObjectBegin();
    bool ObjectTryNext(out Key key);
    T ObjectNext(out Key key);
}

public static class DeserializerExtensions
{
    public static HandleDeserializer<T> GetHandle<T>(this T deserializer)
        where T : IDeserializer<T>
        => new(deserializer);

    public struct HandleDeserializer<T> where T : IDeserializer<T>
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private T deserializer;

        internal HandleDeserializer(T deserializer) => this.deserializer = deserializer;

        public Handle<TEntity> As<TEntity>() where TEntity : struct, IEntity<TEntity> => As(typeof(TEntity)).Strong<TEntity>();

        public Handle As(Type? type)
        {
            deserializer.ArrayBegin();
            var handle = new Handle(type == null ? null : deserializer.World.GetStorageBase(type), deserializer.ArrayNext().GetInt32(), deserializer.ArrayNext().GetInt32());
            _ = deserializer.ArrayTryNext(); //consume the end marker
            return handle;
        }
    }
}
