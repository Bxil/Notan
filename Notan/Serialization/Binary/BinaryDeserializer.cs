using System;
using System.Diagnostics;
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

    public bool GetBoolean()
    {
        AssertTag(BinaryTag.Boolean);
        return reader.ReadBoolean();
    }

    public byte GetByte()
    {
        AssertTag(BinaryTag.Byte);
        return reader.ReadByte();
    }

    public sbyte GetSByte()
    {
        AssertTag(BinaryTag.SByte);
        return reader.ReadSByte();
    }

    public short GetInt16()
    {
        AssertTag(BinaryTag.Int16);
        return reader.ReadInt16();
    }

    public ushort GetUInt16()
    {
        AssertTag(BinaryTag.UInt16);
        return reader.ReadUInt16();
    }

    public int GetInt32()
    {
        AssertTag(BinaryTag.Int32);
        return reader.ReadInt32();
    }

    public uint GetUInt32()
    {
        AssertTag(BinaryTag.UInt32);
        return reader.ReadUInt32();
    }

    public long GetInt64()
    {
        AssertTag(BinaryTag.Int64);
        return reader.ReadInt64();
    }

    public ulong GetUInt64()
    {
        AssertTag(BinaryTag.UInt64);
        return reader.ReadUInt64();
    }

    public float GetSingle()
    {
        AssertTag(BinaryTag.Single);
        return reader.ReadSingle();
    }

    public double GetDouble()
    {
        AssertTag(BinaryTag.Double);
        return reader.ReadDouble();
    }

    public string GetString()
    {
        AssertTag(BinaryTag.String);
        return reader.ReadString();
    }

    public void ArrayBegin()
    {
        AssertTag(BinaryTag.ArrayBegin);
    }

    public bool ArrayTryNext()
    {
        return (BinaryTag)reader.ReadByte() == BinaryTag.ArrayNext;
    }

    public BinaryDeserializer ArrayNext()
    {
        AssertTag(BinaryTag.ArrayNext);
        return this;
    }

    public void ObjectBegin()
    {
        AssertTag(BinaryTag.ObjectBegin);
    }

    public bool ObjectTryNext(out Key key)
    {
        if ((BinaryTag)reader.ReadByte() != BinaryTag.ObjectNext)
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
        AssertTag(BinaryTag.ObjectNext);
        var keylength = reader.Read7BitEncodedInt();
        if (keylength > buffer.Value!.Length)
        {
            Array.Resize(ref buffer.Value, keylength);
        }
        _ = reader.Read(buffer.Value.AsSpan(0, keylength));
        key = new(encoding, buffer.Value.AsSpan(0, keylength));
        return this;
    }

    private void AssertTag(BinaryTag tag)
    {
        var read = (BinaryTag)reader.ReadByte();
        Debug.Assert(read == tag);
    }
}
