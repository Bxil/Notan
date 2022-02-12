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
            context.RegisterForPostInitialization(PostInitialize);
        }

        private static void PostInitialize(GeneratorPostInitializationContext context)
        {
            context.AddSource("Attributes.g.cs",
$@"using System;

#nullable enable

namespace Notan.Serialization;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class AutoSerializeAttribute : Attribute
{{
    public string? Name {{ get; }}

    public AutoSerializeAttribute(string? name = null)
    {{
        Name = name;
    }}
}}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class SerializesAttribute : Attribute
{{
    public Type Type {{ get; }}

    public SerializesAttribute(Type type)
    {{
        Type = type;
    }}
}}");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

            var autoSerializeAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.AutoSerializeAttribute")!;
            var deserializerAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.DeserializerAttribute")!;

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
                foreach (var field in entity.GetMembers().Where(x => HasAttribute(x, autoSerializeAttribute)))
                {
                    var name = '"' + ((string?)GetAttribute(field, autoSerializeAttribute).ConstructorArguments[0].Value ?? field.Name) + '"';
                    var type = GetTypeOfMember(field);
                    _ = builder.Append($"if (key == {name}) {field.Name} = ");
                    if (receiver.Serializes.TryGetValue(type, out var deserializer))
                    {
                        _ = builder.AppendLine($"{deserializer.ToDisplayString()}.Deserialize(entry);");
                    }
                    else if (IsBuiltin(type))
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

                foreach (var field in entity.GetMembers().Where(x => HasAttribute(x, autoSerializeAttribute)))
                {
                    var name = '"' + ((string?)GetAttribute(field, autoSerializeAttribute).ConstructorArguments[0].Value ?? field.Name) + '"';
                    var type = GetTypeOfMember(field);

                    _ = builder.Append($"        ");
                    if (receiver.Serializes.TryGetValue(type, out var serializer))
                    {
                        _ = builder.AppendLine($"{serializer.ToDisplayString()}.Serialize({field.Name}, serializer.ObjectNext({name}));");
                    }
                    else if (IsBuiltin(type))
                    {
                        _ = builder.AppendLine($"serializer.ObjectNext({name}).Write({field.Name});");
                    }
                    else if (type.TypeKind == TypeKind.Enum)
                    {
                        _ = builder.AppendLine($"serializer.ObjectNext({name}).Write(({type.EnumUnderlyingType}){field.Name});");
                    }
                    else
                    {
                        _ = builder.AppendLine($"{field.Name}.Serialize(serializer.ObjectNext({name}));");
                    }
                }
                _ = builder.Append($@"    }}
}}");

                context.AddSource($"{entity.Name}.g.cs", builder.ToString());
                _ = builder.Clear();
            }
        }

        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute) => symbol.GetAttributes().Any(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

        private static AttributeData GetAttribute(ISymbol symbol, INamedTypeSymbol attribute) => symbol.GetAttributes().Single(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

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
            public Dictionary<INamedTypeSymbol, INamedTypeSymbol> Serializes = new(SymbolEqualityComparer.Default);

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var autoSerializeAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.Serialization.AutoSerializeAttribute")!;
                var serializesAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.Serialization.SerializesAttribute")!;
                var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node)!;
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    if (HasAttribute(namedTypeSymbol, autoSerializeAttribute))
                    {
                        Entities.Add(namedTypeSymbol);
                    }
                    if (HasAttribute(namedTypeSymbol, serializesAttribute))
                    {
                        Serializes.Add((INamedTypeSymbol)GetAttribute(namedTypeSymbol, serializesAttribute).ConstructorArguments[0].Value!, namedTypeSymbol);
                    }
                }
            }
        }
    }
}
