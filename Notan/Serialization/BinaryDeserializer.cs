using System.IO;

namespace Notan.Serialization
{
    public struct BinaryDeserializerEntry : IDeserializerEntry<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        public World World { get; }

        private readonly BinaryReader reader;

        public BinaryDeserializerEntry(World world, BinaryReader reader)
        {
            World = world;
            this.reader = reader;
        }

        public bool GetBool() => reader.ReadBoolean();

        public byte GetByte() => reader.ReadByte();

        public short GetInt16() => reader.ReadInt16();

        public int GetInt32() => reader.ReadInt32();

        public long GetInt64() => reader.ReadInt64();

        public float GetSingle() => reader.ReadSingle();

        public double GetDouble() => reader.ReadDouble();

        public string GetString() => reader.ReadString();

        public BinaryDeserializerArray GetArray() => new(World, reader);

        public BinaryDeserializerObject GetObject() => new(World, reader);
    }

    public struct BinaryDeserializerArray : IDeserializerArray<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        private readonly World world;

        private readonly BinaryReader reader;

        public BinaryDeserializerArray(World world, BinaryReader reader)
        {
            this.world = world;
            this.reader = reader;
        }

        public bool Next(out BinaryDeserializerEntry entry)
        {
            if (!reader.ReadBoolean())
            {
                entry = default;
                return false;
            }
            entry = new(world, reader);
            return true;
        }
    }

    public struct BinaryDeserializerObject : IDeserializerObject<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        private readonly World world;

        private readonly BinaryReader reader;

        public BinaryDeserializerObject(World world, BinaryReader reader)
        {
            this.world = world;
            this.reader = reader;
        }

        public bool Next(out string key, out BinaryDeserializerEntry value)
        {
            if (!reader.ReadBoolean())
            {
                key = null!;
                value = default;
                return false;
            }
            key = reader.ReadString();
            value = new(world, reader);
            return true;
        }
    }
}
