using System.IO;

namespace Notan.Serialization
{
    public struct BinarySerializer : ISerializer<BinarySerializer>
    {
        private readonly BinaryWriter writer;

        public BinarySerializer(BinaryWriter writer) => this.writer = writer;

        public void BeginArray(int length) => writer.Write7BitEncodedInt(length);

        public void EndArray() { }

        public void BeginObject() { }

        public void EndObject() { }

        public BinarySerializer Entry(string name) => this;

        public void Write(byte value) => writer.Write(value);

        public void Write(string value) => writer.Write(value);

        public void Write(bool value) => writer.Write(value);

        public void Write(short value) => writer.Write(value);

        public void Write(int value) => writer.Write(value);

        public void Write(long value) => writer.Write(value);

        public void Write(float value) => writer.Write(value);

        public void Write(double value) => writer.Write(value);
    }
}
