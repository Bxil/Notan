using System;
using System.IO;
using System.Text.Json;

namespace Notan.Serialization
{


    public struct JsonDeserializerEntry : IDeserializerEntry<JsonDeserializerEntry, JsonDeserializerArray, JsonDeserializerObject>
    {
        public World World { get; }

        private readonly JsonStream stream;

        public JsonDeserializerEntry(World world, Stream stream)
        {
            World = world;
            this.stream = new(stream);
        }

        internal JsonDeserializerEntry(World world, JsonStream stream)
        {
            World = world;
            this.stream = stream;
        }

        public JsonDeserializerArray GetArray() => new(World, stream);

        public bool GetBool() => stream.Read().GetBoolean();

        public byte GetByte() => stream.Read().GetByte();

        public double GetDouble() => stream.Read().GetDouble();

        public short GetInt16() => stream.Read().GetInt16();

        public int GetInt32() => stream.Read().GetInt32();

        public long GetInt64() => stream.Read().GetInt64();

        public JsonDeserializerObject GetObject() => new(World, stream);

        public float GetSingle() => stream.Read().GetSingle();

        public string GetString() => stream.Read().GetString()!;
    }

    public struct JsonDeserializerArray : IDeserializerArray<JsonDeserializerEntry, JsonDeserializerArray, JsonDeserializerObject>
    {
        private readonly World world;

        private readonly JsonStream stream;

        public JsonDeserializerArray(World world, Stream stream)
        {
            this.world = world;
            this.stream = new(stream);
            Init();
        }

        internal JsonDeserializerArray(World world, JsonStream stream)
        {
            this.world = world;
            this.stream = stream;
            Init();
        }

        private void Init()
        {
            if (stream.Read().TokenType != JsonTokenType.StartArray)
            {
                throw new Exception("Excepted array start.");
            }
        }

        public bool Next(out JsonDeserializerEntry entry)
        {
            var reader = stream.Read(false);
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                stream.Read();
                entry = default;
                return false;
            }
            entry = new(world, stream);
            return true;
        }
    }

    public struct JsonDeserializerObject : IDeserializerObject<JsonDeserializerEntry, JsonDeserializerArray, JsonDeserializerObject>
    {
        private readonly World world;

        private readonly JsonStream stream;

        public JsonDeserializerObject(World world, Stream stream)
        {
            this.world = world;
            this.stream = new(stream);
            Init();
        }

        internal JsonDeserializerObject(World world, JsonStream stream)
        {
            this.world = world;
            this.stream = stream;
            Init();
        }

        private void Init()
        {
            if (stream.Read().TokenType != JsonTokenType.StartObject)
            {
                throw new Exception("Excepted object start.");
            }
        }

        public bool Next(out string key, out JsonDeserializerEntry value)
        {
            var reader = stream.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                key = null!;
                value = default;
                return false;
            }
            key = reader.GetString()!;
            value = new(world, stream);
            return true;
        }
    }

    internal class JsonStream
    {
        private readonly Stream stream;
        private byte[] buffer = new byte[Environment.SystemPageSize];
        private int len = 0;

        private int consume = 0;

        private JsonReaderState state = new();

        public JsonStream(Stream stream)
        {
            this.stream = stream;
        }

        //After this call get a token, and do nothing else with the reader
        public Utf8JsonReader Read(bool consume = true)
        {
            buffer.AsSpan(this.consume, len).CopyTo(buffer);
            len -= this.consume;

            var reader = new Utf8JsonReader(buffer.AsSpan(0, len), false, state);
            while (!reader.Read())
            {
                if (len == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }
                len += stream.Read(buffer, len, buffer.Length - len);
                reader = new(buffer, false, reader.CurrentState);
            }
            this.consume = consume ? (int)reader.BytesConsumed : 0;
            state = consume ? reader.CurrentState : state;
            return reader;
        }
    }
}
