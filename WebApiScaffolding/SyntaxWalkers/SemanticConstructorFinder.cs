using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebApiScaffolding.SyntaxWalkers;

public class SemanticConstructorFinder
{
    private readonly SemanticModel _semanticModel;

    public SemanticConstructorFinder(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public IMethodSymbol? GetBaseConstructorForDerivedClass(ClassDeclarationSyntax derivedClass)
    {
        var derivedClassSymbol = _semanticModel.GetDeclaredSymbol(derivedClass) as INamedTypeSymbol;

        if (derivedClassSymbol?.BaseType == null)
        {
            return null;
        }

        var baseClassSymbol = derivedClassSymbol.BaseType;


        var constructors = baseClassSymbol.Constructors
            .Where(c => c.MethodKind == MethodKind.Constructor && !c.IsStatic)
            .ToList();


        if (derivedClass.ParameterList?.Parameters.Count > 0)
        {
            return FindMatchingConstructor(constructors, derivedClass.ParameterList.Parameters);
        }

        return constructors.FirstOrDefault();
    }

    private IMethodSymbol? FindMatchingConstructor(IEnumerable<IMethodSymbol> constructors, SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        foreach (var constructor in constructors)
        {
            if (constructor.Parameters.Length == parameters.Count)
            {
                bool matches = true;
                for (int i = 0; i < constructor.Parameters.Length; i++)
                {
                    var paramSymbol = constructor.Parameters[i];
                    var paramSyntax = parameters[i];

                    var paramTypeSymbol = _semanticModel.GetTypeInfo(paramSyntax.Type).Type;
                    if (paramTypeSymbol == null || !paramTypeSymbol.Equals(paramSymbol.Type, SymbolEqualityComparer.Default))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches) return constructor;
            }
        }

        return constructors.FirstOrDefault();
    }
}