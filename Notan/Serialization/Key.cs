﻿using System;
using System.Buffers;
using System.Text;

namespace Notan.Serialization;
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
public readonly ref struct Key
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
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

    public static bool operator ==(string left, Key right) => left == right;
    public static bool operator !=(string left, Key right) => !(left == right);

    public override string ToString()
    {
        return encoding.GetString(bytes);
    }
}
