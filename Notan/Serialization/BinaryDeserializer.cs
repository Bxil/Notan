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

        public BinaryDeserializer GetEntry(string name) => this;

        public bool ReadBool() => reader.ReadBoolean();

        public byte ReadByte() => reader.ReadByte();

        public int ReadInt32() => reader.ReadInt32();

        public string ReadString() => reader.ReadString();
    }
}
