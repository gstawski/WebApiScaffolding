using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.SyntaxWalkers;

namespace WebApiScaffolding.SyntaxWalkers;

public class FindConstructorCollector : CSharpSyntaxWalker
{
    private readonly SemanticModel _model;

    public ICollection<SyntaxConstructorMeta> Constructors { get; } = new List<SyntaxConstructorMeta>();

    public FindConstructorCollector(SemanticModel model)
    {
        _model = model;
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Constructors.Add(new SyntaxConstructorMeta(_model, node));
    }

    public override void VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node)
    {
        Constructors.Add(new SyntaxConstructorMeta(_model, node));
    }
}