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

[AttributeUsage(AttributeTargets.Struct)]
internal sealed class GenerateSerializationAttribute : Attribute {{}}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class SerializeAttribute : Attribute
{{
    public string? Name {{ get; }}

    public SerializeAttribute(string? name = null)
    {{
        Name = name;
    }}
}}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class HandleIsAttribute : Attribute
{{
    public Type Type {{ get; }}

    public HandleIsAttribute(Type type)
    {{
        Type = type;
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

            var serializeAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.SerializeAttribute")!;
            var deserializerAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.DeserializerAttribute")!;
            var handleIsAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.HandleIsAttribute")!;
            var ientityType = context.Compilation.GetTypeByMetadataName("Notan.IEntity`1")!;
            var handleType = context.Compilation.GetTypeByMetadataName("Notan.Handle")!;

            var builder = new StringBuilder();
            foreach (var serialized in receiver.Serialized)
            {
                bool isEntity = serialized.AllInterfaces.Contains(ientityType.Construct(serialized));

                _ = builder
                    .AppendLine("using Notan;")
                    .AppendLine("using Notan.Serialization;")
                    .AppendLine("using System.IO;");
                if (serialized.ContainingNamespace != null)
                {
                    _ = builder
                        .AppendLine()
                        .AppendLine($"namespace {serialized.ContainingNamespace};");
                }

                _ = builder.Append($@"
public partial{(serialized.IsRecord ? " record " : " ")}struct {serialized.Name}
{{");

                string deserPrefix = isEntity ? "" : "self.";

                if (!isEntity)
                {
                    _ = builder.Append(
$@"
    public static void Deserialize<T>(ref {serialized.Name} self, T deserializer) where T : IDeserializer<T>
    {{
        deserializer.ObjectBegin();
        while (deserializer.ObjectTryNext(out var key))
        {{
            var entry = deserializer;

");
                }
                else
                {
                    _ = builder.Append(
$@"
    void IEntity<{serialized.Name}>.Deserialize<T>(Key key, T entry)
    {{
");
                }

                string depth = isEntity ? "        " : "            ";
                _ = builder.Append(depth);
                foreach (var field in serialized.GetMembers().Where(x => HasAttribute(x, serializeAttribute)).Cast<IFieldSymbol>())
                {
                    var name = '"' + ((string?)GetAttribute(field, serializeAttribute).ConstructorArguments[0].Value ?? field.Name) + '"';
                    var type = (INamedTypeSymbol)field.Type;
                    _ = builder.Append($"if (key == {name}) ");
                    if (receiver.Serializes.TryGetValue(type, out var deserializer))
                    {
                        _ = builder.AppendLine($"{deserializer.ToDisplayString()}.Deserialize(ref {deserPrefix}{field.Name}, entry);");
                    }
                    else if (IsBuiltin(type))
                    {
                        _ = builder.AppendLine($"{deserPrefix}{field.Name} = entry.Get{type.Name}();");
                    }
                    else if (type.TypeKind == TypeKind.Enum)
                    {
                        _ = builder.AppendLine($"{deserPrefix}{field.Name} = ({type.ToDisplayString()})entry.Get{type.EnumUnderlyingType!.Name}();");
                    }
                    else if (type.Equals(handleType, SymbolEqualityComparer.Default) && HasAttribute(field, handleIsAttribute))
                    {
                        _ = builder.AppendLine($"{deserPrefix}{field.Name} = Handle.Deserialize(entry, typeof({((INamedTypeSymbol)GetAttribute(field, handleIsAttribute).ConstructorArguments[0].Value!).ToDisplayString()}));");
                    }
                    else
                    {
                        _ = builder.AppendLine($"{type.ToDisplayString()}.Deserialize(ref {deserPrefix}{field.Name}, entry);");
                    }
                    _ = builder.Append(depth).Append($"else ");
                }
                _ = builder.Append($"throw new IOException($\"{serialized.Name} has no such field: {{key.ToString()}}.\");");

                if (!isEntity)
                {
                    _ = builder.AppendLine();
                    _ = builder.Append("        }");
                }

                if (!isEntity)
                {
                    _ = builder.Append(@"
    }

    public void Serialize<T>(T serializer) where T : ISerializer<T>
    {
");
                }
                else
                {
                    _ = builder.Append($@"
    }}

    void IEntity<{serialized.Name}>.Serialize<T>(T serializer)
    {{
");
                }

                if (!isEntity)
                {
                    _ = builder.AppendLine("        serializer.ObjectBegin();");
                }

                foreach (var field in serialized.GetMembers().Where(x => HasAttribute(x, serializeAttribute)).Cast<IFieldSymbol>())
                {
                    var name = '"' + ((string?)GetAttribute(field, serializeAttribute).ConstructorArguments[0].Value ?? field.Name) + '"';
                    var type = (INamedTypeSymbol)field.Type;

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

                if (!isEntity)
                {
                    _ = builder.AppendLine($"        serializer.ObjectEnd();");
                }

                _ = builder.Append($@"    }}
}}");

                context.AddSource($"{serialized.ToDisplayString()}.g.cs", builder.ToString());
                _ = builder.Clear();
            }
        }

        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute) => symbol.GetAttributes().Any(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

        private static AttributeData GetAttribute(ISymbol symbol, INamedTypeSymbol attribute) => symbol.GetAttributes().Single(x => attribute.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

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
            public List<INamedTypeSymbol> Serialized = new();
            public Dictionary<INamedTypeSymbol, INamedTypeSymbol> Serializes = new(SymbolEqualityComparer.Default);

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var generateSerializationAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.Serialization.GenerateSerializationAttribute")!;
                var serializesAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.Serialization.SerializesAttribute")!;
                var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node)!;
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    if (HasAttribute(namedTypeSymbol, generateSerializationAttribute))
                    {
                        Serialized.Add(namedTypeSymbol);
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
