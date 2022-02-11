namespace Notan.Serialization;

public interface IDeserializer<T> where T : IDeserializer<T>
{
    public World World { get; }

    bool GetBoolean();
    byte GetByte();
    sbyte GetSByte();
    short GetInt16();
    ushort GetUInt16();
    int GetInt32();
    uint GetUInt32();
    long GetInt64();
    ulong GetUInt64();
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