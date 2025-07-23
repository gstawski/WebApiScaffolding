using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class AnalyzeSolutionService : IAnalyzeSolutionService
{
    private readonly IOptions<AppConfig> _appConfig;
    private readonly CommandLineArgs _commandLineArgs;

    private Dictionary<string,ISymbol> _allProjectSymbols = new();

    private static ClassDeclarationSyntax? GetClassDeclarationSyntax(ISymbol symbol)
    {
        if (symbol is not ITypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
        {
            return null; // Not a class symbol
        }

        var syntaxReferences = symbol.DeclaringSyntaxReferences;

        if (syntaxReferences.Length > 0)
        {
            var syntaxNode = syntaxReferences[0].GetSyntax();

            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration;
            }
        }

        return null;
    }

    public AnalyzeSolutionService(IOptions<AppConfig> appConfig, CommandLineArgs commandLineArgs)
    {
        _appConfig = appConfig;
        _commandLineArgs = commandLineArgs;
    }

    public async Task AnalyzeSolution()
    {
        if (string.IsNullOrEmpty(_commandLineArgs.SolutionPath))
        {
            throw new ArgumentException("Solution path must be provided.");
        }

        var solution = await WorkspaceSolution.Load(_commandLineArgs.SolutionPath);

        _allProjectSymbols = await solution.AllProjectSymbols();

        var domainNamespace = _appConfig.Value.DomainNamespace;
        var className = _commandLineArgs.ClassName;

        foreach (var symbol in _allProjectSymbols.Values)
        {
            var symbolNamespace = symbol.ContainingNamespace.ToString();
            if (symbolNamespace == null)
            {
                continue;
            }

            var symbolName = symbol.Name;
            if (string.IsNullOrEmpty(symbolName))
            {
                continue;
            }

            if (symbolName == className && symbolNamespace.StartsWith(domainNamespace))
            {
                await Console.Out.WriteLineAsync($"Found class: {symbol.Name} in namespace {symbol.ContainingNamespace}");

                var classDeclaration = GetClassDeclarationSyntax(symbol);

                FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector();
                publicPropertiesCollector.Visit(classDeclaration);
                if (publicPropertiesCollector.Properties.Count > 0)
                {
                }
            }
        }
    }
}