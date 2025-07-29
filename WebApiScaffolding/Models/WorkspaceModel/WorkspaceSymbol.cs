using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceSymbol
{
    public SemanticModel Model { get; init; }
    public INamedTypeSymbol Symbol { get; init; }
    public ClassDeclarationSyntax? DeclarationSyntaxForClass { get; init; }
    public EnumDeclarationSyntax? DeclarationSyntaxForEnum { get; init; }

    public string Name => Symbol.Name;
    public string FullName => Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    public string Namespace => Symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    public string UnderlyingGenericTypeName
    {
        get
        {
            if (Symbol.IsGenericType)
            {
                foreach (var type in Symbol.TypeArguments)
                {
                    return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                }
            }

            return string.Empty;
        }
    }
}