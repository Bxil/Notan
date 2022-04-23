using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Notan.Serialization.Binary;

public readonly struct BinaryDeserializer : IDeserializer<BinaryDeserializer>
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
        ConsumeTag(BinaryTag.Boolean);
        return reader.ReadBoolean();
    }

    public byte GetByte()
    {
        ConsumeTag(BinaryTag.Byte);
        return reader.ReadByte();
    }

    public sbyte GetSByte()
    {
        ConsumeTag(BinaryTag.SByte);
        return reader.ReadSByte();
    }

    public short GetInt16()
    {
        ConsumeTag(BinaryTag.Int16);
        return reader.ReadInt16();
    }

    public ushort GetUInt16()
    {
        ConsumeTag(BinaryTag.UInt16);
        return reader.ReadUInt16();
    }

    public int GetInt32()
    {
        ConsumeTag(BinaryTag.Int32);
        return reader.ReadInt32();
    }

    public uint GetUInt32()
    {
        ConsumeTag(BinaryTag.UInt32);
        return reader.ReadUInt32();
    }

    public long GetInt64()
    {
        ConsumeTag(BinaryTag.Int64);
        return reader.ReadInt64();
    }

    public ulong GetUInt64()
    {
        ConsumeTag(BinaryTag.UInt64);
        return reader.ReadUInt64();
    }

    public float GetSingle()
    {
        ConsumeTag(BinaryTag.Single);
        return reader.ReadSingle();
    }

    public double GetDouble()
    {
        ConsumeTag(BinaryTag.Double);
        return reader.ReadDouble();
    }

    public string GetString()
    {
        ConsumeTag(BinaryTag.String);
        return reader.ReadString();
    }

    public void ArrayBegin()
    {
        ConsumeTag(BinaryTag.ArrayBegin);
    }

    public bool ArrayTryNext()
    {
        return !TryConsumeTag(BinaryTag.ArrayEnd);
    }

    public BinaryDeserializer ArrayNext()
    {
        RejectTag(BinaryTag.ArrayEnd);
        return this;
    }

    public void ObjectBegin()
    {
        ConsumeTag(BinaryTag.ObjectBegin);
    }

    public bool ObjectTryNext(out Key key)
    {
        if (TryConsumeTag(BinaryTag.ObjectEnd))
        {
            key = default;
            return false;
        }
        ConsumeTag(BinaryTag.String);
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
        ConsumeTag(BinaryTag.String);
        var keylength = reader.Read7BitEncodedInt();
        if (keylength > buffer.Value!.Length)
        {
            Array.Resize(ref buffer.Value, keylength);
        }
        _ = reader.Read(buffer.Value.AsSpan(0, keylength));
        key = new(encoding, buffer.Value.AsSpan(0, keylength));
        return this;
    }

    private readonly StrongBox<BinaryTag?> tag = new(null);

    private bool TryConsumeTag(BinaryTag tag)
    {
        if (this.tag.Value == null)
        {
            this.tag.Value = (BinaryTag)reader.ReadByte();
        }
        if (this.tag.Value == tag)
        {
            this.tag.Value = null;
            return true;
        }
        return false;
    }

    private void ConsumeTag(BinaryTag tag)
    {
        bool success = TryConsumeTag(tag);
        Debug.Assert(success);
    }

    private void RejectTag(BinaryTag tag)
    {
        bool success = TryConsumeTag(tag);
        Debug.Assert(!success);
    }
}
