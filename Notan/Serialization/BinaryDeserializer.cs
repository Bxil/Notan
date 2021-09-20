using System;
using System.IO;

namespace Notan.Serialization
{
    public struct BinaryDeserializerEntry : IDeserializerEntry<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        public World World { get; }

        private readonly BinaryReader reader;

        private readonly byte[] buffer;

        public BinaryDeserializerEntry(World world, BinaryReader reader)
        {
            World = world;
            this.reader = reader;
            buffer = new byte[64];
        }

        internal BinaryDeserializerEntry(World world, BinaryReader reader, byte[] buffer)
        {
            World = world;
            this.reader = reader;
            this.buffer = buffer;
        }

        public bool GetBool() => reader.ReadBoolean();

        public byte GetByte() => reader.ReadByte();

        public short GetInt16() => reader.ReadInt16();

        public int GetInt32() => reader.ReadInt32();

        public long GetInt64() => reader.ReadInt64();

        public float GetSingle() => reader.ReadSingle();

        public double GetDouble() => reader.ReadDouble();

        public string GetString() => reader.ReadString();

        public BinaryDeserializerArray GetArray() => new(World, reader, buffer);

        public BinaryDeserializerObject GetObject() => new(World, reader, buffer);
    }

    public struct BinaryDeserializerArray : IDeserializerArray<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        private readonly World world;

        private readonly BinaryReader reader;

        private readonly byte[] buffer;

        public BinaryDeserializerArray(World world, BinaryReader reader)
        {
            this.world = world;
            this.reader = reader;
            buffer = new byte[64];
        }

        internal BinaryDeserializerArray(World world, BinaryReader reader, byte[] buffer)
        {
            this.world = world;
            this.reader = reader;
            this.buffer = buffer;
        }

        public bool Next(out BinaryDeserializerEntry entry)
        {
            if (!reader.ReadBoolean())
            {
                entry = default;
                return false;
            }
            entry = new(world, reader, buffer);
            return true;
        }
    }

    public struct BinaryDeserializerObject : IDeserializerObject<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>
    {
        private readonly World world;

        private readonly BinaryReader reader;

        private byte[] buffer;

        public BinaryDeserializerObject(World world, BinaryReader reader)
        {
            this.world = world;
            this.reader = reader;
            buffer = new byte[64];
        }

        internal BinaryDeserializerObject(World world, BinaryReader reader, byte[] buffer)
        {
            this.world = world;
            this.reader = reader;
            this.buffer = buffer;
        }

        public bool Next(out KeyComparison key, out BinaryDeserializerEntry value)
        {
            if (!reader.ReadBoolean())
            {
                key = default;
                value = default;
                return false;
            }
            //TODO: support more than ASCII
            int keylength = reader.Read7BitEncodedInt();
            if (keylength > buffer.Length)
            {
                Array.Resize(ref buffer, keylength);
            }
            reader.Read(buffer.AsSpan(0, keylength));
            key = new(buffer.AsSpan(0, keylength));
            value = new(world, reader);
            return true;
        }
    }
}
