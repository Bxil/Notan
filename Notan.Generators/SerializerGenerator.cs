using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Notan.Generators;

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
        context.AddSource("Attributes.g.cs", SourceText.From(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Notan.Generators.EmbeddedResources.Attributes.cs"), canBeEmbedded: true));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

        var serializeAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.SerializeAttribute")!;
        var handleIsAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.HandleIsAttribute")!;
        var ientityType = context.Compilation.GetTypeByMetadataName("Notan.IEntity`1")!;
        var handleType = context.Compilation.GetTypeByMetadataName("Notan.Handle")!;

        var text = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Notan.Generators.EmbeddedResources.Serialized.cs")).ReadToEnd();

        var propertiesBuilder = new StringBuilder();
        var serializeBuilder = new StringBuilder();
        var deserializeBuilder = new StringBuilder();
        foreach (var serialized in receiver.Serialized)
        {
            string nspace = serialized.ContainingNamespace != null ? $"namespace {serialized.ContainingNamespace};" : "";

            string structtype = serialized.IsRecord ? "record struct" : "struct";

            bool isEntity = serialized.AllInterfaces.Contains(ientityType.Construct(serialized));

            string serializesSignature = isEntity
                ? "void IEntity<__TYPENAME__>.Serialize<T>(T serializer)"
                : "public void Serialize<T>(T serializer) where T : ISerializer<T>";

            string deserializesSignature = isEntity
                ? "void IEntity<__TYPENAME__>.Deserialize<T>(T deserializer)"
                : "public static void Deserialize<T>(ref __TYPENAME__ self, T deserializer) where T : IDeserializer<T>";

            string deserPrefix = isEntity ? "" : "self.";

            _ = serializeBuilder.AppendLine("serializer.ObjectBegin();").Append("        ");
            _ = deserializeBuilder
                .AppendLine("deserializer.ObjectBegin();")
                .AppendLine("        while (deserializer.ObjectTryNext(out var key))")
                .AppendLine("        {")
                .Append("            ");
            foreach (var field in serialized.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.TryGetAttribute(serializeAttribute, out var serializeData))
                {
                    continue;
                }

                if (field.TryGetAttribute(handleIsAttribute, out var handleIsData) && (bool)handleIsData.ConstructorArguments[1].Value! && field.Type.Equals(handleType, SymbolEqualityComparer.Default))
                {
                    string propertyName = (string)serializeData.ConstructorArguments[0].Value!;
                    var typeString = ((INamedTypeSymbol)handleIsData.ConstructorArguments[0].Value!).ToDisplayString();
                    _ = propertiesBuilder.AppendLine().AppendLine($"    public Handle<{typeString}> {propertyName} {{ get => {field.Name}.Strong<{typeString}>(); set => {field.Name} = value; }}");
                }

                var name = $"\"{(string?)serializeData.ConstructorArguments[0].Value ?? field.Name}\"";
                var type = (INamedTypeSymbol)field.Type;
                _ = deserializeBuilder.Append($"if (key == {name}) ");
                if (receiver.Serializes.TryGetValue(type, out var serializer))
                {
                    _ = serializeBuilder.AppendLine($"{serializer.ToDisplayString()}.Serialize({field.Name}, serializer.ObjectNext({name}));");
                    _ = deserializeBuilder.AppendLine($"{serializer.ToDisplayString()}.Deserialize(ref {deserPrefix}{field.Name}, deserializer);");
                }
                else if (type.IsBuiltin())
                {
                    _ = serializeBuilder.AppendLine($"serializer.ObjectNext({name}).Write({field.Name});");
                    _ = deserializeBuilder.AppendLine($"{deserPrefix}{field.Name} = deserializer.Get{type.Name}();");
                }
                else if (type.TypeKind == TypeKind.Enum)
                {
                    _ = serializeBuilder.AppendLine($"serializer.ObjectNext({name}).Write(({type.EnumUnderlyingType}){field.Name});");
                    _ = deserializeBuilder.AppendLine($"{deserPrefix}{field.Name} = ({type.ToDisplayString()})deserializer.Get{type.EnumUnderlyingType!.Name}();");
                }
                else
                {
                    _ = serializeBuilder.AppendLine($"{field.Name}.Serialize(serializer.ObjectNext({name}));");
                    _ = deserializeBuilder.AppendLine($"{type.ToDisplayString()}.Deserialize(ref {deserPrefix}{field.Name}, deserializer);");
                }
                _ = serializeBuilder.Append($"        ");
                _ = deserializeBuilder.Append($"            else ");
            }
            _ = serializeBuilder.Append($"serializer.ObjectEnd();");
            _ = deserializeBuilder.AppendLine($"throw new IOException($\"{serialized.Name} has no such field: {{key.ToString()}}.\");")
                .Append("        }");

            var formatted = text
                .Replace("__NAMESPACE__", nspace)
                .Replace("__STRUCTTYPE__", structtype)
                .Replace("__SERIALIZESIGNATURE__", serializesSignature)
                .Replace("__DESERIALIZESIGNATURE__", deserializesSignature)
                .Replace("__PROPERTIES__", propertiesBuilder.ToString())
                .Replace("__SERIALIZE__", serializeBuilder.ToString())
                .Replace("__DESERIALIZE__", deserializeBuilder.ToString())
                .Replace("__TYPENAME__", serialized.Name);

            context.AddSource($"{serialized.ToDisplayString()}.g.cs", formatted);
            _ = propertiesBuilder.Clear();
            _ = serializeBuilder.Clear();
            _ = deserializeBuilder.Clear();
        }
    }

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
                if (namedTypeSymbol.TryGetAttribute(generateSerializationAttribute, out _))
                {
                    Serialized.Add(namedTypeSymbol);
                }
                foreach (var serializesData in namedTypeSymbol.GetAttributes(serializesAttribute))
                {
                    Serializes.Add((INamedTypeSymbol)serializesData.ConstructorArguments[0].Value!, namedTypeSymbol);
                }
            }
        }
    }
}
