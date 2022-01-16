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
    public static Handle GetHandle<T, TEntity>(this T deserializer)
        where T : IDeserializer<T>
        where TEntity : struct, IEntity<TEntity>
    {
        deserializer.ArrayBegin();
        var handle = new Handle(deserializer.World.GetStorageBase<TEntity>(), deserializer.ArrayNext().GetInt32(), deserializer.ArrayNext().GetInt32());
        _ = deserializer.ArrayTryNext(); //consume the end marker
        return handle;
    }
}
