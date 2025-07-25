using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.SyntaxWalkers;

public sealed class FindPublicPropertiesCollector : CSharpSyntaxWalker
{
    private readonly SemanticModel _model;
    private int _order;
    public ICollection<PropertyMeta> Properties { get; } = new List<PropertyMeta>();

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

    public FindPublicPropertiesCollector(SemanticModel model)
    {
        _model = model;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var isPublic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
        var isStatic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
        var isSetPublic = false;
        var isCollection = false;

        if (node.AccessorList != null && node.AccessorList.Accessors.Count > 0)
        {
            foreach (var accessor in node.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) && IsPublic(accessor))
                {
                    isSetPublic = true;
                }
            }
        }

        if (isPublic && !isStatic)
        {
            var name = node.Identifier.Text;
            var typeName = node.Type.ToString();

            var isSimpleType = node.Type is PredefinedTypeSyntax ||
                               (node.Type is NullableTypeSyntax nullableType && nullableType.ElementType is PredefinedTypeSyntax);

            if (!isSimpleType)
            {
                if (typeName.EndsWith("DateTime") || typeName.EndsWith("DateTime?"))
                {
                    isSimpleType = true;
                }
                else if (typeName.EndsWith("DateTimeOffset") || typeName.EndsWith("DateTimeOffset?"))
                {
                    isSimpleType = true;
                }
                else if (typeName.EndsWith("TimeSpan") || typeName.EndsWith("TimeSpan?"))
                {
                    isSimpleType = true;
                }

                isCollection = IsCollectionType(node.Type, _model);
            }

            Properties.Add(new PropertyMeta
            {
                Name = name,
                Type = typeName,
                Order = ++_order,
                IsSetPublic = isSetPublic,
                IsSimpleType = isSimpleType,
                IsCollection = isCollection
            });
        }
    }
}