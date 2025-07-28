using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Models.SyntaxWalkers;

public class SyntaxPropertyMeta
{
    private readonly PropertyDeclarationSyntax _propertyDeclaration;

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

    private static bool IsCollectionType(TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
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
               typeSymbol.Name.EndsWith("Collection") ||
               typeSymbol.Name.EndsWith("List");
    }

    private static bool DatesAreSimpleTypes(string typeName)
    {
        if (typeName.EndsWith("DateOnly") || typeName.EndsWith("DateOnly?"))
        {
            return true;
        }
        if (typeName.EndsWith("DateTime") || typeName.EndsWith("DateTime?"))
        {
            return true;
        }
        if (typeName.EndsWith("DateTimeOffset") || typeName.EndsWith("DateTimeOffset?"))
        {
            return true;
        }
        if (typeName.EndsWith("TimeSpan") || typeName.EndsWith("TimeSpan?"))
        {
            return true;
        }

        return false;
    }

    public SyntaxPropertyMeta(SemanticModel model, PropertyDeclarationSyntax node, int order)
    {
        Order = order;
        _propertyDeclaration = node;

        if (node.AccessorList != null && node.AccessorList.Accessors.Count > 0)
        {
            foreach (var accessor in node.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) && IsPublic(accessor))
                {
                    IsSetPublic = true;
                }
            }
        }

        IsSimpleType = node.Type is PredefinedTypeSyntax ||
                           (node.Type is NullableTypeSyntax nullableType && nullableType.ElementType is PredefinedTypeSyntax);
        if (!IsSimpleType)
        {
            if (DatesAreSimpleTypes(Type))
            {
                IsSimpleType = true;
            }

            IsCollection = IsCollectionType(node.Type, model);
        }
    }

    public string Name => _propertyDeclaration.Identifier.Text;

    public string Type => _propertyDeclaration.Type.ToString();

    public bool IsSimpleType { get; set; }

    public int Order { get; }

    public bool IsSetPublic { get; }

    public bool IsCollection { get; set; }

    public PropertyMeta ToPropertyMeta()
    {
        return new PropertyMeta
        {
            Name = Name,
            Type = Type,
            IsSimpleType = IsSimpleType,
            Order = Order,
            IsSetPublic = IsSetPublic,
            IsCollection = IsCollection
        };
    }
}