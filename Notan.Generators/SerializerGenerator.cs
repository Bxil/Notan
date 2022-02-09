using Microsoft.CodeAnalysis;
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
sealed class AutoSerializeAttribute : Attribute
{{
    public Type? Generic {{ get; }}
    public AutoSerializeAttribute(Type? generic = null)
    {{
        Generic = generic;
    }}
}}");

            var builder = new StringBuilder();
            foreach (var entity in receiver.Entities)
            {
                _ = builder
                    .AppendLine("using Notan;")
                    .AppendLine("using Notan.Serialization;")
                    .AppendLine("using System.IO;")
                    .AppendLine()
                    .AppendLine($"namespace {entity.ContainingNamespace.ToDisplayString()};");

                _ = builder.Append($@"
public partial struct {entity.Name}
{{
    void IEntity<{entity.Name}>.Deserialize<T>(Key key, T entry)
    {{
");
                _ = builder.Append("        ");
                foreach (var field in entity.GetMembers().Where(x => HasAutoSerialize(x)))
                {
                    INamedTypeSymbol type = null!;
                    if (field is IFieldSymbol fieldSymbol)
                    {
                        type = (INamedTypeSymbol)fieldSymbol.Type;
                    }
                    else if (field is IPropertySymbol propertySymbol)
                    {
                        type = (INamedTypeSymbol)propertySymbol.Type;
                    }

                    _ = builder.Append($"if (key == nameof({field.Name})) {field.Name} = ");
                    if (type.IsGenericType)
                    {
                        //Todo: only append the namespace if it's not already included
                        _ = builder.AppendLine($"entry.Get{type.Name}().As<{string.Join(",", type.TypeArguments.Select(x => x.ContainingNamespace + "." + x.Name))}>();");
                    }
                    else
                    {
                        _ = builder.AppendLine($"entry.Get{type.Name}();");
                    }
                    _ = builder.Append($"        else ");
                }
                _ = builder.Append($"throw new IOException($\"{entity.Name} has no such field: {{key.ToString()}}.\");");
                _ = builder.Append($@"
    }}

    void IEntity<{entity.Name}>.Serialize<T>(T serializer)
    {{");

                foreach (var field in entity.GetMembers().Where(x => HasAutoSerialize(x)))
                {
                    INamedTypeSymbol type = null!;
                    if (field is IFieldSymbol fieldSymbol)
                    {
                        type = (INamedTypeSymbol)fieldSymbol.Type;
                    }
                    else if (field is IPropertySymbol propertySymbol)
                    {
                        type = (INamedTypeSymbol)propertySymbol.Type;
                    }

                    _ = builder.AppendLine().Append($"        serializer.ObjectNext(nameof({field.Name})).");
                    if (type.IsGenericType)
                    {
                        _ = builder.AppendLine($"Write().As({field.Name});");
                    }
                    else
                    {
                        _ = builder.AppendLine($"Write({field.Name});");
                    }
                }
                _ = builder.Append($@"    }}
}}");

                context.AddSource($"{entity.Name}.g.cs", builder.ToString());
                _ = builder.Clear();
            }
        }

        private static bool HasAutoSerialize(ISymbol syntax) => syntax.GetAttributes().Any(x => x.AttributeClass!.Name == "AutoSerialize");

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
