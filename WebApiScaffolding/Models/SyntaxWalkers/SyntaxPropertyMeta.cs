using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Models.SyntaxWalkers;

public class SyntaxPropertyMeta
{
    private readonly PropertyDeclarationSyntax _propertyDeclaration;

    private readonly ITypeSymbol? _typeSymbol;

    private static bool IsPublic(AccessorDeclarationSyntax accessor)
    {
        foreach (SyntaxToken token in accessor.Modifiers)
        {
            if (token.IsKind(SyntaxKind.PrivateKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCollectionType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        if (typeSymbol.TypeKind == TypeKind.Array)
        {
            return true;
        }

        var enumerableInterfaces = typeSymbol.AllInterfaces
            .Where(i => i.Name == "IEnumerable" &&
                        (i.IsGenericType || i.Name == "IEnumerable"));

        return enumerableInterfaces.Any() ||
               typeSymbol.Name.EndsWith("Collection", StringComparison.Ordinal) ||
               typeSymbol.Name.EndsWith("List", StringComparison.Ordinal);
    }

    private static bool SystemTypeSimpleTypeCheck(string typeName)
    {
        if (typeName.EndsWith("Guid", StringComparison.Ordinal) || typeName.EndsWith("Guid?", StringComparison.Ordinal))
        {
            return true;
        }
        if (typeName.EndsWith("DateOnly", StringComparison.Ordinal) || typeName.EndsWith("DateOnly?", StringComparison.Ordinal))
        {
            return true;
        }
        if (typeName.EndsWith("DateTime", StringComparison.Ordinal) || typeName.EndsWith("DateTime?", StringComparison.Ordinal))
        {
            return true;
        }
        if (typeName.EndsWith("DateTimeOffset", StringComparison.Ordinal) || typeName.EndsWith("DateTimeOffset?", StringComparison.Ordinal))
        {
            return true;
        }
        if (typeName.EndsWith("TimeSpan", StringComparison.Ordinal) || typeName.EndsWith("TimeSpan?", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    public SyntaxPropertyMeta(SemanticModel model, PropertyDeclarationSyntax node, int order)
    {
        Order = order;
        _propertyDeclaration = node;
        _typeSymbol = model.GetTypeInfo(node.Type).Type;

        IsSimpleType = node.Type is PredefinedTypeSyntax ||
                           (node.Type is NullableTypeSyntax nullableType && nullableType.ElementType is PredefinedTypeSyntax);
        if (!IsSimpleType)
        {
            if (SystemTypeSimpleTypeCheck(Type))
            {
                IsSimpleType = true;
            }

            IsCollection = IsCollectionType(_typeSymbol);
        }
    }

    public string Name => _propertyDeclaration.Identifier.Text;

    public string Type => _propertyDeclaration.Type.ToString();

    public bool IsSimpleType { get; }

    public int Order { get; }

    public bool IsCollection { get; }

    public string FullName => _typeSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty;

    public string UnderlyingGenericTypeName
    {
        get
        {
            if (_typeSymbol != null)
            {
                var symbol = (INamedTypeSymbol)_typeSymbol;
                if (symbol.IsGenericType)
                {
                    foreach (var type in symbol.TypeArguments)
                    {
                        return type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                    }
                }
            }

            return string.Empty;
        }
    }

    public PropertyMeta ToPropertyMeta()
    {
        return new PropertyMeta
        {
            Name = Name,
            Type = Type,
            IsSimpleType = IsSimpleType,
            Order = Order,
            IsCollection = IsCollection
        };
    }
}