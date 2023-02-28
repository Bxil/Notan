using System;
using System.Buffers;
using System.Text;

namespace Notan.Serialization;

public readonly ref struct Key
{
    private readonly Encoding encoding;
    private readonly ReadOnlySpan<byte> bytes;

    public Key(Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        this.encoding = encoding;
        this.bytes = bytes;
    }

    public static bool operator ==(Key left, string right)
    {
        var buffer = ArrayPool<char>.Shared.Rent(left.encoding.GetMaxCharCount(left.bytes.Length));
        var count = left.encoding.GetChars(left.bytes, buffer);
        var ok = ((ReadOnlySpan<char>)buffer[..count]).Equals(right, StringComparison.Ordinal);
        ArrayPool<char>.Shared.Return(buffer);
        return ok;
    }

    public static bool operator !=(Key left, string right) => !(left == right);

    public static bool operator ==(string left, Key right) => right == left;
    public static bool operator !=(string left, Key right) => !(left == right);

    public override string ToString()
    {
        return encoding.GetString(bytes);
    }
}
