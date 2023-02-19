using System;
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

    public void Deserialize(ref bool value)
    {
        ConsumeTag(BinaryTag.Boolean);
        value = reader.ReadBoolean();
    }

    public void Deserialize(ref byte value)
    {
        ConsumeTag(BinaryTag.Byte);
        value = reader.ReadByte();
    }

    public void Deserialize(ref sbyte value)
    {
        ConsumeTag(BinaryTag.SByte);
        value = reader.ReadSByte();
    }

    public void Deserialize(ref short value)
    {
        ConsumeTag(BinaryTag.Int16);
        value = reader.ReadInt16();
    }

    public void Deserialize(ref ushort value)
    {
        ConsumeTag(BinaryTag.UInt16);
        value = reader.ReadUInt16();
    }

    public void Deserialize(ref int value)
    {
        ConsumeTag(BinaryTag.Int32);
        value = reader.ReadInt32();
    }

    public void Deserialize(ref uint value)
    {
        ConsumeTag(BinaryTag.UInt32);
        value = reader.ReadUInt32();
    }

    public void Deserialize(ref long value)
    {
        ConsumeTag(BinaryTag.Int64);
        value = reader.ReadInt64();
    }

    public void Deserialize(ref ulong value)
    {
        ConsumeTag(BinaryTag.UInt64);
        value = reader.ReadUInt64();
    }

    public void Deserialize(ref float value)
    {
        ConsumeTag(BinaryTag.Single);
        value = reader.ReadSingle();
    }

    public void Deserialize(ref double value)
    {
        ConsumeTag(BinaryTag.Double);
        value = reader.ReadDouble();
    }

    public void Deserialize(ref string value)
    {
        ConsumeTag(BinaryTag.String);
        value = reader.ReadString();
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
        this.tag.Value ??= (BinaryTag)reader.ReadByte();
        if (this.tag.Value == tag)
        {
            this.tag.Value = null;
            return true;
        }
        return false;
    }

    private void ConsumeTag(BinaryTag tag)
    {
        if (!TryConsumeTag(tag))
        {
            NotanException.Throw($"Expected tag '{tag}' but found '{this.tag}'");
        }
    }

    private void RejectTag(BinaryTag tag)
    {
        if (TryConsumeTag(tag))
        {
            NotanException.Throw($"Expected different tag from '{tag}'");
        }
    }
}
