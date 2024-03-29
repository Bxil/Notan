﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Notan.Generators;

public static class SymbolExtensions
{
    public static bool TryGetAttribute(this ISymbol symbol, INamedTypeSymbol attribute, out AttributeData data)
    {
        var attributes = symbol.GetAttributes();
        data = attributes.FirstOrDefault(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default))!;
        return data != null;
    }

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().Where(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default));
    }

    public static bool IsBuiltin(this INamedTypeSymbol symbol)
        => symbol.ToDisplayString() is
            "bool" or
            "byte" or "sbyte" or
            "short" or "ushort" or
            "int" or "uint" or
            "long" or "ulong" or
            "float" or "double" or
            "string";
}
