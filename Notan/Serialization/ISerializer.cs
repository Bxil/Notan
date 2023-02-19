using System;
using System.Runtime.CompilerServices;

namespace Notan.Serialization;

public interface ISerializer<T> where T : ISerializer<T>
{
    void Serialize(bool value);
    void Serialize(byte value);
    void Serialize(sbyte value);
    void Serialize(short value);
    void Serialize(ushort value);
    void Serialize(int value);
    void Serialize(uint value);
    void Serialize(long value);
    void Serialize(ulong value);
    void Serialize(float value);
    void Serialize(double value);
    void Serialize(string value);
    void ArrayBegin();
    T ArrayNext();
    void ArrayEnd();
    void ObjectBegin();
    T ObjectNext(string key);
    void ObjectEnd();
}

public static class SerializerSerializable
{
    public static void Serialize<TSer, T>(this TSer serializer, in T value)
        where TSer : ISerializer<TSer>
        where T : ISerializable
    {
        value.Serialize(serializer);
    }
}

public static class SerializerEnum
{
    public static void Serialize<TSer, T>(this TSer serializer, T value)
        where TSer : ISerializer<TSer>
        where T : Enum
    {
        switch (Unsafe.SizeOf<T>())
        {
            case 1:
                serializer.Serialize(Unsafe.As<T, byte>(ref value));
                break;
            case 2:
                serializer.Serialize(Unsafe.As<T, short>(ref value));
                break;
            case 4:
                serializer.Serialize(Unsafe.As<T, int>(ref value));
                break;
            case 8:
                serializer.Serialize(Unsafe.As<T, long>(ref value));
                break;
            default:
                throw new NotSupportedException("Enums must be of size 1, 2, 4, or 8");
        }
    }
}