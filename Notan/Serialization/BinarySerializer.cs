using System.IO;

namespace Notan.Serialization
{
    public struct BinarySerializer : ISerializer
    {
        private readonly BinaryWriter writer;

        public BinarySerializer(BinaryWriter writer) => this.writer = writer;

        public void BeginArray(int length) => writer.Write7BitEncodedInt(length);

        public void EndArray() { }

        public void BeginObject() { }

        public void EndObject() { }

        public void Write(string name, byte value) => writer.Write(value);

        public void Write(string name, string value) => writer.Write(value);

        public void Write(string name, bool value) => writer.Write(value);

        public void Write(string name, int value) => writer.Write(value);

        public void WriteEntry(string name) { }
    }
}
