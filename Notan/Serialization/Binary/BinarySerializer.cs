using System.IO;
using System.Text;

namespace Notan.Serialization.Binary;

public readonly struct BinarySerializer : ISerializer<BinarySerializer>
{
    private readonly BinaryWriter writer;

    public BinarySerializer(Stream stream, Encoding encoding) => writer = new BinaryWriter(stream, encoding, true);

    public void Write(bool value)
    {
        WriteTag(BinaryTag.Boolean);
        writer.Write(value);
    }

    public void Write(byte value)
    {
        WriteTag(BinaryTag.Byte);
        writer.Write(value);
    }

    public void Write(sbyte value)
    {
        WriteTag(BinaryTag.SByte);
        writer.Write(value);
    }

    public void Write(short value)
    {
        WriteTag(BinaryTag.Int16);
        writer.Write(value);
    }

    public void Write(ushort value)
    {
        WriteTag(BinaryTag.UInt16);
        writer.Write(value);
    }

    public void Write(int value)
    {
        WriteTag(BinaryTag.Int32);
        writer.Write(value);
    }

    public void Write(uint value)
    {
        WriteTag(BinaryTag.UInt32);
        writer.Write(value);
    }

    public void Write(long value)
    {
        WriteTag(BinaryTag.Int64);
        writer.Write(value);
    }

    public void Write(ulong value)
    {
        WriteTag(BinaryTag.UInt64);
        writer.Write(value);
    }

    public void Write(float value)
    {
        WriteTag(BinaryTag.Single);
        writer.Write(value);
    }

    public void Write(double value)
    {
        WriteTag(BinaryTag.Double);
        writer.Write(value);
    }

    public void Write(string value)
    {
        WriteTag(BinaryTag.String);
        writer.Write(value);
    }

    public void ArrayBegin()
    {
        WriteTag(BinaryTag.ArrayBegin);
    }

    public BinarySerializer ArrayNext()
    {
        return this;
    }

    public void ArrayEnd()
    {
        WriteTag(BinaryTag.ArrayEnd);
    }

    public void ObjectBegin()
    {
        WriteTag(BinaryTag.ObjectBegin);
    }

    public BinarySerializer ObjectNext(string key)
    {
        Write(key);
        return this;
    }

    public void ObjectEnd()
    {
        WriteTag(BinaryTag.ObjectEnd);
    }

    private void WriteTag(BinaryTag tag) => writer.Write((byte)tag);
}
