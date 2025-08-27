using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceSolution : WorkspaceBase
{
    private readonly Solution _solution;
    private readonly Action<string> _logAction;

    private readonly IDictionary<string, WorkspaceProject> _openProjects = new Dictionary<string, WorkspaceProject>();

    private static bool IsValidNamespace(ISymbol symbol, string domainNamespace)
    {
        if (symbol.Kind == SymbolKind.NamedType)
        {
            var namedTypeSymbol = (INamedTypeSymbol)symbol;
            return namedTypeSymbol.ContainingNamespace
                .ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                .StartsWith(domainNamespace, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsInheritingFromSymbol(ISymbol symbol, INamedTypeSymbol symbolToFindUsage)
    {
        if (symbol.Kind == SymbolKind.NamedType)
        {
            var namedTypeSymbol = (INamedTypeSymbol)symbol;
            if (namedTypeSymbol.BaseType == null)
            {
                return false;
            }

            var baseType = namedTypeSymbol.BaseType;

            if (baseType.ConstructedFrom.Equals(symbol, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (baseType.IsGenericType)
            {
                var symbolToFindName = symbolToFindUsage.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

                foreach (var type in baseType.TypeArguments)
                {
                    if (type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) == symbolToFindName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static async Task<WorkspaceSolution> Load(string fileName, Action<string> logAction)
    {
        logAction("Loading solution...");
        var workspace = BuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(fileName, new WorkspaceProgressBarProjectLoadStatus(logAction));

        return new WorkspaceSolution(solution, logAction);
    }

    private WorkspaceSolution(Solution solution, Action<string> logAction)
    {
        _solution = solution;
        _logAction = logAction;
    }

    public async Task<Dictionary<string, WorkspaceSymbol>> AllProjectSymbols()
    {
        foreach (var p in _solution.Projects)
        {
            var project = await WorkspaceProject.LoadFromSolution(p, _logAction);
            if (project != null)
            {
                _openProjects[p.Name] = project;
            }
        }

        Dictionary<string, WorkspaceSymbol> allSymbols = new Dictionary<string, WorkspaceSymbol>();

        foreach (var p in _openProjects.Values)
        {
            var sym = await p.AllProjectSymbols();

            foreach (var item in sym)
            {
                var key = item.FullName;
                if (!allSymbols.TryAdd(key, item))
                {
                    _logAction($"Duplicate: {key}");
                }
            }
        }

        return allSymbols;
    }

    public async Task<List<WorkspacePropertyUsage>> FindPropertiesUsingDomainTypes(string symbolName, string domainNamespace)
    {
        var propertiesUsingDomainTypes = new List<WorkspacePropertyUsage>();

        foreach (var p in _solution.Projects)
        {
            if (p.Name.StartsWith(domainNamespace, StringComparison.Ordinal))
            {
                foreach (var document in p.Documents)
                {
                    var root = await document.GetSyntaxRootAsync();
                    if (root == null)
                    {
                        continue;
                    }

                    var semanticModel = await document.GetSemanticModelAsync();
                    if (semanticModel == null)
                    {
                        continue;
                    }

                    foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    {
                        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
                        {
                            var typeSymbol = semanticModel.GetTypeInfo(property.Type).Type;

                            if (typeSymbol != null
                                && typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) == symbolName
                                && IsValidNamespace(typeSymbol, domainNamespace))
                            {
                                propertiesUsingDomainTypes.Add(new WorkspacePropertyUsage
                                {
                                    ClassName = classDeclaration.Identifier.Text,
                                    PropertyName = property.Identifier.Text,
                                    TypeName = typeSymbol.ToDisplayString(),
                                    Location = property.GetLocation().ToString()
                                });
                            }
                        }
                    }
                }

                break;
            }
        }

        return propertiesUsingDomainTypes;
    }

    public async Task<List<WorkspaceClassUsage>> FindClassesInheritingFrom(INamedTypeSymbol symbolToFindUsage, string domainNamespace)
    {
        var classesInheritingFromEntity = new List<WorkspaceClassUsage>();

        foreach (var p in _solution.Projects)
        {
            if (p.Name.StartsWith(domainNamespace, StringComparison.Ordinal))
            {
                foreach (var document in p.Documents)
                {
                    var root = await document.GetSyntaxRootAsync();
                    if (root == null)
                    {
                        continue;
                    }

                    var semanticModel = await document.GetSemanticModelAsync();
                    if (semanticModel == null)
                    {
                        continue;
                    }

                    foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);

                        if (symbol != null && IsInheritingFromSymbol(symbol, symbolToFindUsage))
                        {
                            classesInheritingFromEntity.Add(new WorkspaceClassUsage
                            {
                                ClassName = symbol.Name,
                                FullClassName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                Location = classDeclaration.GetLocation().ToString()
                            });
                        }
                    }
                }
            }
        }

        return classesInheritingFromEntity;
    }
}