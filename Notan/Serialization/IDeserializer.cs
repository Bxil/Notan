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

public static class DeserializerSugar
{
    public static bool DeserializeBool<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out bool value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static byte DeserializeByte<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out byte value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static sbyte DeserializeSByte<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out sbyte value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static short DeserializeInt16<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out short value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static ushort DeserializeUInt16<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out ushort value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static int DeserializeInt32<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out int value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static uint DeserializeUInt32<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out uint value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static long DeserializeInt64<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out long value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static ulong DeserializeUInt64<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out ulong value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static float DeserializeSingle<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out float value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static double DeserializeDouble<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out double value);
        deserializer.Deserialize(ref value);
        return value;
    }

    public static string DeserializeString<T>(this T deserializer)
        where T : IDeserializer<T>
    {
        Unsafe.SkipInit(out string value);
        deserializer.Deserialize(ref value);
        return value;
    }
}