namespace Notan.Serialization;

public interface ISerializer<T> where T : ISerializer<T>
{
    void Write(bool value);
    void Write(byte value);
    void Write(sbyte value);
    void Write(short value);
    void Write(ushort value);
    void Write(int value);
    void Write(uint value);
    void Write(long value);
    void Write(ulong value);
    void Write(float value);
    void Write(double value);
    void Write(string value);
    void ArrayBegin();
    T ArrayNext();
    void ArrayEnd();
    void ObjectBegin();
    T ObjectNext(string key);
    void ObjectEnd();
}

public static class SerializerExtensions
{
    public static void Serialize<TSer, T>(this TSer serializer, in T value)
        where TSer : ISerializer<TSer>
        where T : ISerializable
    {
        value.Serialize(serializer);
    }
}