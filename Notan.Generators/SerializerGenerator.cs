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
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
        {
            return;
        }

        var serializeAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.SerializeAttribute")!;
        var handleIsAttribute = context.Compilation.GetTypeByMetadataName("Notan.Serialization.HandleIsAttribute")!;
        var handleType = context.Compilation.GetTypeByMetadataName("Notan.Handle")!;

        var text = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Notan.Generators.EmbeddedResources.Serialized.cs")).ReadToEnd();

        var propertiesBuilder = new StringBuilder();
        var serializeBuilder = new StringBuilder();
        var deserializeBuilder = new StringBuilder();
        foreach (var serialized in receiver.Serialized)
        {
            var nspace = serialized.ContainingNamespace != null ? $"namespace {serialized.ContainingNamespace};" : "";

            var structtype = serialized.IsRecord ? "record struct" : "struct";

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
                    var propertyName = (string)serializeData.ConstructorArguments[0].Value!;
                    var typeString = ((INamedTypeSymbol)handleIsData.ConstructorArguments[0].Value!).ToDisplayString();
                    _ = propertiesBuilder.AppendLine().AppendLine($"    public Handle<{typeString}> {propertyName} {{ get => {field.Name}.Strong<{typeString}>(); set => {field.Name} = value; }}");
                }

                var name = $"\"{(string?)serializeData.ConstructorArguments[0].Value ?? field.Name}\"";
                var type = (INamedTypeSymbol)field.Type;
                _ = deserializeBuilder.Append($"if (key == {name}) ");
                if (type.IsBuiltin())
                {
                    _ = serializeBuilder.AppendLine($"serializer.ObjectNext({name}).Write({field.Name});");
                    _ = deserializeBuilder.AppendLine($"{field.Name} = deserializer.Get{type.Name}();");
                }
                else if (type.TypeKind == TypeKind.Enum)
                {
                    _ = serializeBuilder.AppendLine($"serializer.ObjectNext({name}).Write(({type.EnumUnderlyingType}){field.Name});");
                    _ = deserializeBuilder.AppendLine($"{field.Name} = ({type.ToDisplayString()})deserializer.Get{type.EnumUnderlyingType!.Name}();");
                }
                else
                {
                    _ = serializeBuilder.AppendLine($"serializer.ObjectNext({name}).Serialize({field.Name});");
                    _ = deserializeBuilder.AppendLine($"deserializer.Deserialize(ref {field.Name});");
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

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var generateSerializationAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.Serialization.GenerateSerializationAttribute")!;
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node)!;
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.TryGetAttribute(generateSerializationAttribute, out _))
                {
                    Serialized.Add(namedTypeSymbol);
                }
            }
        }
    }
}
