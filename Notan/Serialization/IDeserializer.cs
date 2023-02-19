using System.Runtime.CompilerServices;
using System;

namespace Notan.Serialization;

public interface IDeserializer<T> where T : IDeserializer<T>
{
    public World World { get; }

    void Deserialize(ref bool value);
    void Deserialize(ref byte value);
    void Deserialize(ref sbyte value);
    void Deserialize(ref short value);
    void Deserialize(ref ushort value);
    void Deserialize(ref int value);
    void Deserialize(ref uint value);
    void Deserialize(ref long value);
    void Deserialize(ref ulong value);
    void Deserialize(ref float value);
    void Deserialize(ref double value);
    void Deserialize(ref string value);

    void ArrayBegin();
    bool ArrayTryNext();
    T ArrayNext();

    void ObjectBegin();
    bool ObjectTryNext(out Key key);
    T ObjectNext(out Key key);
}

public static class DeserializerSerializable
{
    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref T value)
        where TDeser : IDeserializer<TDeser>
        where T : ISerializable
    {
        value.Deserialize(deserializer);
    }
}

public static class DeserailizerEnum
{
    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref T value)
        where TDeser : IDeserializer<TDeser>
        where T : Enum
    {
        switch (Unsafe.SizeOf<T>())
        {
            case 1:
                deserializer.Deserialize(ref Unsafe.As<T, byte>(ref value));
                break;
            case 2:
                deserializer.Deserialize(ref Unsafe.As<T, short>(ref value));
                break;
            case 4:
                deserializer.Deserialize(ref Unsafe.As<T, int>(ref value));
                break;
            case 8:
                deserializer.Deserialize(ref Unsafe.As<T, long>(ref value));
                break;
            default:
                throw new NotSupportedException("Enums must be of size 1, 2, 4, or 8");
        }
    }
}