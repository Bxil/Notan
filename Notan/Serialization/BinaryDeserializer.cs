using System.IO;

namespace Notan.Serialization
{
    public struct BinaryDeserializer : IDeserializer<BinaryDeserializer>
    {
        public World World { get; set; }

        private readonly BinaryReader reader;

        public BinaryDeserializer(World world, BinaryReader reader)
        {
            World = world;
            this.reader = reader;
        }

        public int BeginArray() => reader.Read7BitEncodedInt();

        public BinaryDeserializer NextArrayElement() => this;

        public BinaryDeserializer Entry(string name) => this;

        public bool TryGetEntry(string name, out BinaryDeserializer entry)
        {
            entry = Entry(name);
            return true;
        }

        public bool ReadBool() => reader.ReadBoolean();

        public byte ReadByte() => reader.ReadByte();

        public short ReadInt16() => reader.ReadInt16();

        public int ReadInt32() => reader.ReadInt32();

        public long ReadInt64() => reader.ReadInt64();

        public string ReadString() => reader.ReadString();

        public float ReadSingle() => reader.ReadSingle();

        public double ReadDouble() => ReadDouble();
    }
}
