using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Notan.Serialization;

public sealed class JsonDeserializer : IDeserializer<JsonDeserializer>
{
    public World World { get; }

    private readonly Stream stream;
    private byte[] buffer = new byte[Environment.SystemPageSize];
    private int len = 0;

    private int consume = 0;

    private JsonReaderState state = new();

    public JsonDeserializer(World world, Stream stream)
    {
        World = world;
        this.stream = stream;
    }

    //After this call get a token, and do nothing else with the reader
    private Utf8JsonReader Read(bool consume = true)
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

    public bool GetBool() => Read().GetBoolean();

    public byte GetByte() => Read().GetByte();

    public double GetDouble() => Read().GetDouble();

    public short GetInt16() => Read().GetInt16();

    public int GetInt32() => Read().GetInt32();

    public long GetInt64() => Read().GetInt64();

    public float GetSingle() => Read().GetSingle();

    public string GetString() => Read().GetString()!;

    public void ArrayBegin()
    {
        if (Read().TokenType != JsonTokenType.StartArray)
        {
            throw new Exception("Excepted array start.");
        }
    }

    public bool ArrayTryNext()
    {
        var reader = Read(false);
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            _ = Read();
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
        if (Read().TokenType != JsonTokenType.StartObject)
        {
            throw new Exception("Excepted object start.");
        }
    }

    public bool ObjectTryNext(out Key key)
    {
        var reader = Read();
        if (reader.TokenType == JsonTokenType.EndObject)
        {
            key = default;
            return false;
        }
        key = new(Encoding.UTF8, buffer.AsSpan((int)reader.TokenStartIndex + 1, (int)(reader.BytesConsumed - reader.TokenStartIndex - 3)));
        return true;
    }

    public JsonDeserializer ObjectNext(out Key key)
    {
        return ObjectTryNext(out key) ? this : throw new IOException("Array has no more elements.");
    }
}