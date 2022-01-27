using System;
using System.Buffers;
using System.Text;

namespace Notan.Serialization;
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public readonly ref struct Key
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    private readonly ReadOnlySpan<byte> utf8;

    public Key(ReadOnlySpan<byte> utf8)
    {
        this.utf8 = utf8;
    }

    public static bool operator ==(Key left, string right)
    {
        int i = 0;
        int bytecount = 0;
        while (i < right.Length && Rune.DecodeFromUtf8(left.utf8[bytecount..], out var rune, out var bytes) == OperationStatus.Done)
        {
            int charsread = 1;
            if (!Rune.TryCreate(right[i], out var rrune))
            {
                if (!Rune.TryCreate(right[i], right[i + 1], out rrune))
                {
                    return false;
                }
                charsread = 2;
            }

            if (rune != rrune)
            {
                return false;
            }
            bytecount += bytes;
            i += charsread;
        }
        return true;
    }

    public static bool operator !=(Key left, string right) => !(left == right);

    public static bool operator ==(string left, Key right) => left == right;
    public static bool operator !=(string left, Key right) => !(left == right);

    public override string ToString()
    {
        return Encoding.UTF8.GetString(utf8);
    }
}
