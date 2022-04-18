using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Notan.Serialization.Binary;

public struct BinaryDeserializer : IDeserializer<BinaryDeserializer>
{
    public World World { get; }

    private readonly BinaryReader reader;
    private readonly Encoding encoding;

    private readonly StrongBox<byte[]> buffer = new(new byte[64]);

    public BinaryDeserializer(World world, Stream stream, Encoding encoding)
    {
        World = world;
        reader = new BinaryReader(stream, encoding, true);
        this.encoding = encoding;
    }

    public bool GetBoolean() => reader.ReadBoolean();

    public byte GetByte() => reader.ReadByte();

    public sbyte GetSByte() => reader.ReadSByte();

    public short GetInt16() => reader.ReadInt16();

    public ushort GetUInt16() => reader.ReadUInt16();

    public int GetInt32() => reader.ReadInt32();

    public uint GetUInt32() => reader.ReadUInt32();

    public long GetInt64() => reader.ReadInt64();

    public ulong GetUInt64() => reader.ReadUInt64();

    public float GetSingle() => reader.ReadSingle();

    public double GetDouble() => reader.ReadDouble();

    public string GetString() => reader.ReadString();

    public void ArrayBegin() { }

    public bool ArrayTryNext() => reader.ReadBoolean();

    public BinaryDeserializer ArrayNext()
    {
        return ArrayTryNext() ? this : throw new IOException("Array has no more elements.");
    }

    public void ObjectBegin() { }

    public bool ObjectTryNext(out Key key)
    {
        if (!reader.ReadBoolean())
        {
            key = default;
            return false;
        }
        var keylength = reader.Read7BitEncodedInt();
        if (keylength > buffer.Value!.Length)
        {
            Array.Resize(ref buffer.Value, keylength);
        }
        _ = reader.Read(buffer.Value.AsSpan(0, keylength));
        key = new(encoding, buffer.Value.AsSpan(0, keylength));
        return true;
    }

    public BinaryDeserializer ObjectNext(out Key key)
    {
        return ObjectTryNext(out key) ? this : throw new IOException("Array has no more elements.");
    }
}
