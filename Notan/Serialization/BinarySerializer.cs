using System.IO;
using System.Text;

namespace Notan.Serialization;

public struct BinarySerializer : ISerializer<BinarySerializer>
{
    private readonly BinaryWriter writer;

    public BinarySerializer(Stream stream, Encoding encoding) => writer = new BinaryWriter(stream, encoding, true);

    public void Write(bool value) => writer.Write(value);

    public void Write(byte value) => writer.Write(value);

    public void Write(short value) => writer.Write(value);

    public void Write(int value) => writer.Write(value);

    public void Write(long value) => writer.Write(value);

    public void Write(float value) => writer.Write(value);

    public void Write(double value) => writer.Write(value);

    public void Write(string value) => writer.Write(value);

    public void ArrayBegin() { }

    public BinarySerializer ArrayNext()
    {
        writer.Write(true);
        return this;
    }

    public void ArrayEnd()
    {
        writer.Write(false);
    }

    public void ObjectBegin() { }

    public BinarySerializer ObjectNext(string key)
    {
        writer.Write(true);
        writer.Write(key);
        return this;
    }

    public void ObjectEnd()
    {
        writer.Write(false);
    }
}
