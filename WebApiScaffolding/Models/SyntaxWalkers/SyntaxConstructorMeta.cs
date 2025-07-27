using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Models.SyntaxWalkers;

public class SyntaxConstructorMeta
{
    private readonly SemanticModel _semanticModel;
    private readonly ConstructorDeclarationSyntax? _constructorDeclaration;
    private readonly PrimaryConstructorBaseTypeSyntax? _primaryConstructorDeclaration;

    public SyntaxConstructorMeta(SemanticModel semanticModel, ConstructorDeclarationSyntax constructorDeclaration)
    {
        _semanticModel = semanticModel;
        _constructorDeclaration = constructorDeclaration;
    }

    public SyntaxConstructorMeta(SemanticModel semanticModel, PrimaryConstructorBaseTypeSyntax constructorDeclaration)
    {
        _semanticModel = semanticModel;
        _primaryConstructorDeclaration = constructorDeclaration;
    }

    public List<IPropertySymbol> GetPropertiesSetInConstructor(Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        if (_constructorDeclaration != null)
        {
            return GetPropertiesSetInConstructor(_constructorDeclaration, _semanticModel);
        }

        if (_primaryConstructorDeclaration != null)
        {
            return GetPropertiesSetInConstructor(_primaryConstructorDeclaration, _semanticModel, findSymbolByName);
        }

        return new List<IPropertySymbol>();
    }

    private static IPropertySymbol? GetPropertySymbol(
        PrimaryConstructorBaseTypeSyntax baseTypeSyntax,
        SemanticModel semanticModel,
        string propertyName)
    {
        var baseTypeSymbol = ModelExtensions.GetTypeInfo(semanticModel, baseTypeSyntax.Type).Type;
        if (baseTypeSymbol == null)
        {
            return null;
        }

        return baseTypeSymbol.GetMembers(propertyName)
            .OfType<IPropertySymbol>()
            .FirstOrDefault();
    }

    private static List<IPropertySymbol> GetBaseClassPrimaryConstructor( PrimaryConstructorBaseTypeSyntax baseTypeSyntax, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        var typeName = baseTypeSyntax.Type.ToString();

        if (typeName.Contains("<"))
        {
            typeName = typeName.Substring(0, typeName.IndexOf("<", StringComparison.OrdinalIgnoreCase));
        }

        var symbol = findSymbolByName(typeName);

        if (symbol == null)
        {
            return new List<IPropertySymbol>();
        }

        var visitor = new FindConstructorCollector(symbol.Model);
        visitor.Visit(symbol.DeclarationSyntaxForClass);

        if (visitor.Constructors.Count > 0)
        {
            var constructorSyntax = visitor.Constructors.First();
            return constructorSyntax.GetPropertiesSetInConstructor(findSymbolByName);
        }

        return new List<IPropertySymbol>();
    }

    private static List<IPropertySymbol> GetPropertiesSetInConstructor(PrimaryConstructorBaseTypeSyntax constructor, SemanticModel semanticModel, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        var propertyAssignments = new List<IPropertySymbol>();

        foreach (var argument in constructor.ArgumentList.Arguments)
        {
            if (argument.NameColon != null)
            {
                // Handle named argument syntax (PropertyName: value)
                var propertyName = argument.NameColon.Name.Identifier.ValueText;
                var propertySymbol = GetPropertySymbol(constructor, semanticModel, propertyName);
                if (propertySymbol != null)
                {
                    propertyAssignments.Add(propertySymbol);
                }
            }
            else if (argument.Expression is AssignmentExpressionSyntax assignment)
            {
                // Handle assignment syntax (PropertyName = value)
                if (assignment.Left is IdentifierNameSyntax identifier)
                {
                    var propertyName = identifier.Identifier.ValueText;
                    var propertySymbol = GetPropertySymbol(constructor, semanticModel, propertyName);
                    if (propertySymbol != null)
                    {
                        propertyAssignments.Add(propertySymbol);
                    }
                }
            }
        }

        if (propertyAssignments.Count == 0)
        {
            return GetBaseClassPrimaryConstructor(constructor, findSymbolByName);
        }

        return propertyAssignments;
    }

    private static List<IPropertySymbol> GetPropertiesSetInConstructor(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel)
    {
        var propertyAssignments = new List<IPropertySymbol>();

        if (constructor.Body != null)
        {
            foreach (var statement in constructor.Body.Statements)
            {
                var assignmentExpression = statement as ExpressionStatementSyntax;

                if (assignmentExpression?.Expression is AssignmentExpressionSyntax assignment)
                {
                    var leftHandSideSymbol = ModelExtensions.GetSymbolInfo(semanticModel, assignment.Left).Symbol as IPropertySymbol;

                    if (leftHandSideSymbol != null && IsPropertySetFromParameter(assignment.Right, constructor, semanticModel))
                    {
                        propertyAssignments.Add(leftHandSideSymbol);
                    }
                }
            }
        }

        return propertyAssignments;
    }

    private static bool IsPropertySetFromParameter(ExpressionSyntax rightHandSide, ConstructorDeclarationSyntax constructor, SemanticModel semanticModel)
    {
        var rightHandSideSymbol = ModelExtensions.GetSymbolInfo(semanticModel, rightHandSide).Symbol;

        if (rightHandSideSymbol is IParameterSymbol parameterSymbol)
        {
            return constructor.ParameterList.Parameters.Any(param => param.Identifier.Text == parameterSymbol.Name);
        }

        return false;
    }
}