using System.IO;

namespace Notan.Serialization
{
    public struct BinarySerializerEntry : ISerializerEntry<BinarySerializerEntry, BinarySerializerArray, BinarySerializerObject>
    {
        private readonly BinaryWriter writer;

        public BinarySerializerEntry(BinaryWriter writer) => this.writer = writer;

        public void Write(bool value) => writer.Write(value);

        public void Write(byte value) => writer.Write(value);

        public void Write(short value) => writer.Write(value);

        public void Write(int value) => writer.Write(value);

        public void Write(long value) => writer.Write(value);

        public void Write(float value) => writer.Write(value);

        public void Write(double value) => writer.Write(value);

        public void Write(string value) => writer.Write(value);

        public BinarySerializerArray WriteArray() => new(writer);

        public BinarySerializerObject WriteObject() => new(writer);
    }

    public struct BinarySerializerArray : ISerializerArray<BinarySerializerEntry, BinarySerializerArray, BinarySerializerObject>
    {
        private readonly BinaryWriter writer;

        public BinarySerializerArray(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public BinarySerializerEntry Next()
        {
            writer.Write(true);
            return new(writer);
        }

        public void End()
        {
            writer.Write(false);
        }
    }

    public struct BinarySerializerObject : ISerializerObject<BinarySerializerEntry, BinarySerializerArray, BinarySerializerObject>
    {
        private readonly BinaryWriter writer;

        public BinarySerializerObject(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public BinarySerializerEntry Next(string key)
        {
            writer.Write(true);
            writer.Write(key);
            return new(writer);
        }

        public void End()
        {
            writer.Write(false);
        }
    }
}
