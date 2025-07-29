using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.WorkspaceModel;

namespace WebApiScaffolding.SyntaxWalkers;

internal static class SyntaxHelpers
{
    private static bool IsInheritingFrom(ITypeSymbol? symbol, string baseTypeName)
    {
        if (symbol == null)
        {
            return false;
        }

        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var dotBaseTypeName = $".{baseTypeName}";

        if (fullName.EndsWith(dotBaseTypeName) || fullName == baseTypeName)
        {
            return true;
        }

        var baseTypeSymbol = symbol.BaseType;

        while (baseTypeSymbol != null)
        {
            fullName = baseTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName.EndsWith(dotBaseTypeName) || fullName == baseTypeName)
            {
                return true;
            }

            baseTypeSymbol = baseTypeSymbol.BaseType;
        }

        foreach (var interfaceSymbol in symbol.Interfaces)
        {
            fullName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName.EndsWith(dotBaseTypeName) || fullName == baseTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ExtractNavigationProperties(MethodDeclarationSyntax configureMethod)
    {
        var navigationCalls = configureMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name.Identifier.Text == "Navigation");

        return navigationCalls.SelectMany(i =>
        {
            if (i.ArgumentList.Arguments.Count > 0)
            {
                var argument = i.ArgumentList.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    if (argument.Expression is IdentifierNameSyntax identifierName)
                    {
                        return [identifierName.Identifier.Text];
                    }

                    var lambda = argument.Expression as SimpleLambdaExpressionSyntax;
                    if (lambda?.Body is MemberAccessExpressionSyntax memberAccess)
                    {
                        return [memberAccess.Name.Identifier.Text];
                    }
                }
            }

            return Enumerable.Empty<string>();
        });
    }

    public static Dictionary<string, int> GetPropertiesWithNavigation(WorkspaceSymbol? symbol)
    {
        if (symbol != null && symbol.DeclarationSyntaxForClass != null)
        {
            var classDeclaration = symbol.DeclarationSyntaxForClass;

            var configureMethod = classDeclaration.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "Configure");

            if (configureMethod != null)
            {
                var navigationProperties = ExtractNavigationProperties(configureMethod);

                return navigationProperties.ToDictionary(x => x, _ => 0);
            }
        }

        return new Dictionary<string, int>();
    }

    public static bool IsClassInheritingFrom(WorkspaceSymbol workspaceSymbol, string baseTypeName)
    {
        var classDeclaration = workspaceSymbol.DeclarationSyntaxForClass;

        if (classDeclaration == null)
        {
            return false;
        }

        var baseTypeNames = classDeclaration.BaseList?.Types.Select(type => type.Type);

        if (baseTypeNames == null || !baseTypeNames.Any())
        {
            return false;
        }

        var semanticModel = workspaceSymbol.Model;
        foreach (var baseType in baseTypeNames)
        {
            var typeInfo = semanticModel.GetTypeInfo(baseType);
            var resolvedSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
            if (resolvedSymbol != null)
            {
                if (IsInheritingFrom(resolvedSymbol, baseTypeName))
                {
                    return true;
                }
            }
        }

        if (semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol)
        {
            foreach (var interfaceImpl in classSymbol.Interfaces)
            {
                if (IsInheritingFrom(interfaceImpl, baseTypeName))
                {
                    return true;
                }
            }

            if (classSymbol.BaseType != null && IsInheritingFrom(classSymbol.BaseType, baseTypeName))
            {
                return true;
            }
        }

        return false;
    }

    public static (string Namespace, string ClassName) SplitFullName(string input)
    {
        int lastDotIndex = input.LastIndexOf('.');

        if (lastDotIndex == -1 || lastDotIndex == 0 || lastDotIndex == input.Length - 1)
            throw new FormatException("Input must contain at least one dot!");

        string prefix = input.Substring(0, lastDotIndex);
        string suffix = input.Substring(lastDotIndex + 1);

        return (prefix, suffix);
    }
}