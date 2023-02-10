using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Notan;

public class NotanException : Exception
{
    private NotanException(string message) : base(message) { }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(string message)
    {
        throw new NotanException(message);
    }
}