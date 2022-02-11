using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Notan.Generators
{
    [Generator]
    public class SerializerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

            context.AddSource("AutoSerializeAttribute.g.cs",
$@"using System;

#nullable enable

namespace Notan.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
sealed class AutoSerializeAttribute : Attribute {{}}");
            var builder = new StringBuilder();
            foreach (var entity in receiver.Entities)
            {
                _ = builder
                    .AppendLine("using Notan;")
                    .AppendLine("using Notan.Serialization;")
                    .AppendLine("using System.IO;");
                if (entity.ContainingNamespace != null)
                {
                    _ = builder
                        .AppendLine()
                        .AppendLine($"namespace {entity.ContainingNamespace};");
                }

                _ = builder.Append($@"
public partial{(entity.IsRecord ? " record " : " ")}struct {entity.Name}
{{
    void IEntity<{entity.Name}>.Deserialize<T>(Key key, T entry)
    {{
");
                _ = builder.Append("        ");
                foreach (var field in entity.GetMembers().Where(x => HasAutoSerialize(x)))
                {
                    var type = GetTypeOfMember(field);
                    _ = builder.Append($"if (key == nameof({field.Name})) {field.Name} = ");
                    if (IsBuiltin(type))
                    {
                        _ = builder.AppendLine($"entry.Get{type.Name}();");
                    }
                    else if (type.TypeKind == TypeKind.Enum)
                    {
                        _ = builder.AppendLine($"({type.ToDisplayString()})entry.Get{type.EnumUnderlyingType!.Name}();");
                    }
                    else
                    {
                        _ = builder.AppendLine($"{type.ToDisplayString()}.Deserialize(entry);");
                    }
                    _ = builder.Append($"        else ");
                }
                _ = builder.Append($"throw new IOException($\"{entity.Name} has no such field: {{key.ToString()}}.\");");
                _ = builder.Append($@"
    }}

    void IEntity<{entity.Name}>.Serialize<T>(T serializer)
    {{
");

                foreach (var field in entity.GetMembers().Where(x => HasAutoSerialize(x)))
                {
                    var type = GetTypeOfMember(field);

                    _ = builder.Append($"        ");
                    if (IsBuiltin(type))
                    {
                        _ = builder.AppendLine($"serializer.ObjectNext(nameof({field.Name})).Write({field.Name});");
                    }
                    else if (type.TypeKind == TypeKind.Enum)
                    {
                        _ = builder.AppendLine($"serializer.ObjectNext(nameof({field.Name})).Write(({type.EnumUnderlyingType}){field.Name});");
                    }
                    else
                    {
                        _ = builder.AppendLine($"{field.Name}.Serialize(serializer.ObjectNext(nameof({field.Name})));");
                    }
                }
                _ = builder.Append($@"    }}
}}");

                context.AddSource($"{entity.Name}.g.cs", builder.ToString());
                _ = builder.Clear();
            }
        }

        private static bool HasAutoSerialize(ISymbol syntax) => syntax.GetAttributes().Any(x => x.AttributeClass!.Name == "AutoSerialize");

        private static INamedTypeSymbol GetTypeOfMember(ISymbol symbol)
        {
            if (symbol is IFieldSymbol fieldSymbol)
            {
                return (INamedTypeSymbol)fieldSymbol.Type;
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return (INamedTypeSymbol)propertySymbol.Type;
            }
            throw new Exception();
        }

        private static bool IsBuiltin(INamedTypeSymbol symbol)
            => symbol.ToDisplayString() is
                "bool" or
                "byte" or "sbyte" or
                "short" or "ushort" or
                "int" or "uint" or
                "long" or "ulong" or
                "float" or "double" or
                "string";

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<INamedTypeSymbol> Entities = new();
            public INamedTypeSymbol IEntitySymbol = null!;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node)!;
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    if (HasAutoSerialize(namedTypeSymbol))
                    {
                        Entities.Add(namedTypeSymbol);
                    }
                }
            }
        }
    }
}
