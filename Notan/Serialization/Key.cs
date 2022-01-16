using System;
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
        //TODO: Implement for more than just ascii
        if (left.utf8.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.utf8.Length; i++)
        {
            if (left.utf8[i] != right[i])
            {
                return false;
            }
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
