using System;
using System.IO;

namespace Notan.Serialization
{
    public struct BinaryDeserializer : IDeserializer<BinaryDeserializer>
    {
        public World World { get; }

        private readonly BinaryReader reader;

        private byte[] buffer;

        public BinaryDeserializer(World world, BinaryReader reader)
        {
            World = world;
            this.reader = reader;
            buffer = new byte[64];
        }

        public bool GetBool() => reader.ReadBoolean();

        public byte GetByte() => reader.ReadByte();

        public short GetInt16() => reader.ReadInt16();

        public int GetInt32() => reader.ReadInt32();

        public long GetInt64() => reader.ReadInt64();

        public float GetSingle() => reader.ReadSingle();

        public double GetDouble() => reader.ReadDouble();

        public string GetString() => reader.ReadString();

        public void ArrayBegin() { }

        public bool ArrayTryNext() => reader.ReadBoolean();

        public BinaryDeserializer ArrayNext()
        {
            return ArrayTryNext() ? this : throw new IOException("Array has no more elements.");
        }

        public void ObjectBegin() { }

        public bool ObjectTryNext(out Key key)
        {
            if (!reader.ReadBoolean())
            {
                key = default;
                return false;
            }
            //TODO: support more than ASCII
            var keylength = reader.Read7BitEncodedInt();
            if (keylength > buffer.Length)
            {
                Array.Resize(ref buffer, keylength);
            }
            _ = reader.Read(buffer.AsSpan(0, keylength));
            key = new(buffer.AsSpan(0, keylength));
            return true;
        }

        public BinaryDeserializer ObjectNext(out Key key)
        {
            return ObjectTryNext(out key) ? this : throw new IOException("Array has no more elements.");
        }
    }
}
