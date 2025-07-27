using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.SyntaxWalkers;

namespace WebApiScaffolding.SyntaxWalkers;

public sealed class FindPublicPropertiesCollector : CSharpSyntaxWalker
{
    private readonly SemanticModel _model;
    private int _order;

    public ICollection<SyntaxPropertyMeta> Properties { get; } = new List<SyntaxPropertyMeta>();

    public FindPublicPropertiesCollector(SemanticModel model)
    {
        _model = model;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var isPublic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
        var isStatic = node.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));

        if (isPublic && !isStatic)
        {
            var prop = new SyntaxPropertyMeta(_model, node, _order++);
            Properties.Add(prop);
        }
    }
}