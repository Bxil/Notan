using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Notan.Serialization.Json;

public readonly struct JsonDeserializer : IDeserializer<JsonDeserializer>
{
    public World World { get; }

    private readonly JsonStream stream;

    public JsonDeserializer(World world, Stream stream)
    {
        World = world;
        this.stream = new(stream);
    }

    public void Deserialize(ref bool value) => value = stream.Read().GetBoolean();

    public void Deserialize(ref byte value) => value = stream.Read().GetByte();

    public void Deserialize(ref sbyte value) => value = stream.Read().GetSByte();

    public void Deserialize(ref double value) => value = stream.Read().GetDouble();

    public void Deserialize(ref short value) => value = stream.Read().GetInt16();

    public void Deserialize(ref ushort value) => value = stream.Read().GetUInt16();

    public void Deserialize(ref int value) => value = stream.Read().GetInt32();

    public void Deserialize(ref uint value) => value = stream.Read().GetUInt32();

    public void Deserialize(ref long value) => value = stream.Read().GetInt64();

    public void Deserialize(ref ulong value) => value = stream.Read().GetUInt64();

    public void Deserialize(ref float value) => value = stream.Read().GetSingle();

    public void Deserialize(ref string value) => value = stream.Read().GetString()!;

    public void ArrayBegin()
    {
        if (stream.Read().TokenType != JsonTokenType.StartArray)
        {
            NotanException.Throw("Excepted array start.");
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
        if (!ArrayTryNext())
        {
            NotanException.Throw("Array has no more elements.");
        }
        return this;
    }

    public void ObjectBegin()
    {
        if (stream.Read().TokenType != JsonTokenType.StartObject)
        {
            NotanException.Throw("Excepted object start.");
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
        key = new(stream.Span((int)reader.TokenStartIndex + 1, (int)(reader.BytesConsumed - reader.TokenStartIndex - 3)));
        return true;
    }

    public JsonDeserializer ObjectNext(out Key key)
    {
        if (!ObjectTryNext(out key))
        {
            NotanException.Throw("Object has no more elements.");
        }
        return this;
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