namespace Notan.Serialization;

public interface ISerializer<T> where T : ISerializer<T>
{
    void Write(bool value);
    void Write(byte value);
    void Write(short value);
    void Write(int value);
    void Write(long value);
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
    public static void Write<T>(this ISerializer<T> serializer, Handle handle) where T : ISerializer<T>
    {
        serializer.ArrayBegin();
        serializer.ArrayNext().Write(handle.Index);
        serializer.ArrayNext().Write(handle.Generation);
        serializer.ArrayEnd();
    }
}
