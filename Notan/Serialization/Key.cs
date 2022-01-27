using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;

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
        var utf8 = left.utf8;
        ReadOnlySpan<char> utf16 = right;

        Span<char> utf16Buf = stackalloc char[16];

        do
        {
            if (Utf8.ToUtf16(utf8, utf16Buf, out int read8, out int written16)
                is not OperationStatus.Done and not OperationStatus.DestinationTooSmall)
                throw new Exception("Failed to convert UTF-8 to UTF-16.");

            if (written16 > right.Length)
                break;

            if (!utf16[..written16].Equals(utf16Buf[..written16], StringComparison.InvariantCulture))
                break;

            utf16 = utf16[written16..];
            utf8 = utf8[read8..];
        }
        while (utf8.Length > 0);

        return utf8.IsEmpty;
    }

    public static bool operator !=(Key left, string right) => !(left == right);

    public static bool operator ==(string left, Key right) => left == right;
    public static bool operator !=(string left, Key right) => !(left == right);

    public override string ToString()
    {
        return Encoding.UTF8.GetString(utf8);
    }
}
