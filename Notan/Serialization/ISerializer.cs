using System.Diagnostics.CodeAnalysis;

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
    public static void Write<T>(this ISerializer<T> serializer, Handle handle) where T : ISerializer<T>
    {
        serializer.ArrayBegin();
        serializer.ArrayNext().Write(handle.Index);
        serializer.ArrayNext().Write(handle.Generation);
        serializer.ArrayEnd();
    }

    public static HandleSerializer<T> Write<T>(this T serializer) where T : ISerializer<T>
        => new(serializer);

    public struct HandleSerializer<T> where T : ISerializer<T>
    {
        [SuppressMessage("Style", "IDE0044:Add readonly modifier")]
        private T serializer;

        internal HandleSerializer(T serializer) => this.serializer = serializer;

        public void As<TEntity>(Handle<TEntity> handle) where TEntity : struct, IEntity<TEntity> => serializer.Write(handle);
    }
}