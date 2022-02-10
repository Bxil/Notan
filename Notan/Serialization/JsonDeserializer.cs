using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Notan.Serialization;

public struct JsonDeserializer : IDeserializer<JsonDeserializer>
{
    public World World { get; }

    private readonly JsonStream stream;

    public JsonDeserializer(World world, Stream stream)
    {
        World = world;
        this.stream = new(stream);
    }

    public bool GetBoolean() => stream.Read().GetBoolean();

    public byte GetByte() => stream.Read().GetByte();

    public sbyte GetSByte() => stream.Read().GetSByte();

    public double GetDouble() => stream.Read().GetDouble();

    public short GetInt16() => stream.Read().GetInt16();

    public ushort GetUInt16() => stream.Read().GetUInt16();

    public int GetInt32() => stream.Read().GetInt32();

    public uint GetUInt32() => stream.Read().GetUInt32();

    public long GetInt64() => stream.Read().GetInt64();

    public ulong GetUInt64() => stream.Read().GetUInt64();

    public float GetSingle() => stream.Read().GetSingle();

    public string GetString() => stream.Read().GetString()!;

    public void ArrayBegin()
    {
        if (stream.Read().TokenType != JsonTokenType.StartArray)
        {
            throw new Exception("Excepted array start.");
        }
    }

    public bool ArrayTryNext()
    {
        var reader = stream.Read(false);
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            _ = stream.Read();
            return false;
        }
        return true;
    }

    public JsonDeserializer ArrayNext()
    {
        return ArrayTryNext() ? this : throw new IOException("Array has no more elements.");
    }

    public void ObjectBegin()
    {
        if (stream.Read().TokenType != JsonTokenType.StartObject)
        {
            throw new Exception("Excepted object start.");
        }
    }

    public bool ObjectTryNext(out Key key)
    {
        var reader = stream.Read();
        if (reader.TokenType == JsonTokenType.EndObject)
        {
            key = default;
            return false;
        }
        key = new(Encoding.UTF8, stream.Span((int)reader.TokenStartIndex + 1, (int)(reader.BytesConsumed - reader.TokenStartIndex - 3)));
        return true;
    }

    public JsonDeserializer ObjectNext(out Key key)
    {
        return ObjectTryNext(out key) ? this : throw new IOException("Array has no more elements.");
    }

    private class JsonStream
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
            buffer.AsSpan(this.consume, len - this.consume).CopyTo(buffer);
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

        public Span<byte> Span(int from, int length) => buffer.AsSpan(from, length);
    }

}