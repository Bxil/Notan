using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Notan.Generators
{
    [Generator]
    public class SerializerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

            foreach (var item in receiver.Entities)
            {
                var builder = new StringBuilder();
                context.AddSource($"{item.Identifier}.g.cs", builder.ToString());
            }
        }

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<StructDeclarationSyntax> Entities = new();
            public INamedTypeSymbol IEntitySymbol = null!;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                IEntitySymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Notan.IEntity`1")!;

                if (context.Node is StructDeclarationSyntax structDeclaration
                    && structDeclaration.Modifiers.Any(x => x.ValueText == "partial"))
                {
                    var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(structDeclaration)!;
                    if (symbol.AllInterfaces.Contains(IEntitySymbol.Construct(symbol)))
                    {
                        Entities.Add(structDeclaration);
                    }
                }
            }
        }
    }
}
