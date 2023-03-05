using System;
using System.Buffers;
using System.Text;
using System.Text.Unicode;

namespace Notan.Serialization;

public readonly ref struct Key
{
    private readonly ReadOnlySpan<byte> utf8;

    public Key(ReadOnlySpan<byte> utf8)
    {
        this.utf8 = utf8;
    }

    public static bool operator ==(Key left, string right)
    {
        var utf8 = left.utf8;
        var utf16 = right.AsSpan();
        Span<char> buffer = stackalloc char[16];

        do
        {
            var status = Utf8.ToUtf16(utf8, buffer, out var read8, out var written16);
            if (status is not OperationStatus.Done and not OperationStatus.DestinationTooSmall ||
                written16 > utf16.Length ||
                !utf16[..written16].SequenceEqual(buffer[..written16]))
            {
                break;
            }

            utf16 = utf16[written16..];
            utf8 = utf8[read8..];
        }
        while (utf8.Length > 0);

        return utf8.IsEmpty;
    }

    public static bool operator !=(Key left, string right) => !(left == right);

    public static bool operator ==(string left, Key right) => right == left;
    public static bool operator !=(string left, Key right) => !(left == right);

    public override string ToString() => Encoding.UTF8.GetString(utf8);
}
