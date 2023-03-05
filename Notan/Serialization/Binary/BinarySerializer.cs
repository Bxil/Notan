using System.IO;
using System.Text;

namespace Notan.Serialization.Binary;

public readonly struct BinarySerializer : ISerializer<BinarySerializer>
{
    private readonly BinaryWriter writer;

    public BinarySerializer(Stream stream) => writer = new BinaryWriter(stream, Encoding.UTF8, true);

    public void Serialize(bool value)
    {
        WriteTag(BinaryTag.Boolean);
        writer.Write(value);
    }

    public void Serialize(byte value)
    {
        WriteTag(BinaryTag.Byte);
        writer.Write(value);
    }

    public void Serialize(sbyte value)
    {
        WriteTag(BinaryTag.SByte);
        writer.Write(value);
    }

    public void Serialize(short value)
    {
        WriteTag(BinaryTag.Int16);
        writer.Write(value);
    }

    public void Serialize(ushort value)
    {
        WriteTag(BinaryTag.UInt16);
        writer.Write(value);
    }

    public void Serialize(int value)
    {
        WriteTag(BinaryTag.Int32);
        writer.Write(value);
    }

    public void Serialize(uint value)
    {
        WriteTag(BinaryTag.UInt32);
        writer.Write(value);
    }

    public void Serialize(long value)
    {
        WriteTag(BinaryTag.Int64);
        writer.Write(value);
    }

    public void Serialize(ulong value)
    {
        WriteTag(BinaryTag.UInt64);
        writer.Write(value);
    }

    public void Serialize(float value)
    {
        WriteTag(BinaryTag.Single);
        writer.Write(value);
    }

    public void Serialize(double value)
    {
        WriteTag(BinaryTag.Double);
        writer.Write(value);
    }

    public void Serialize(string value)
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
        Serialize(key);
        return this;
    }

    public void ObjectEnd()
    {
        WriteTag(BinaryTag.ObjectEnd);
    }

    private void WriteTag(BinaryTag tag) => writer.Write((byte)tag);
}
