using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class AnalyzeSolutionService : IAnalyzeSolutionService
{
    private readonly ILogger<AnalyzeSolutionService> _logger;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly IGenerateCodeService _generateCodeService;
    private readonly CommandLineArgs _commandLineArgs;

    private Dictionary<string, WorkspaceSymbol> _allProjectSymbols = new();

    private static bool InheritsFrom(ClassDeclarationSyntax? classDeclaration, string baseTypeName)
    {
        if (classDeclaration == null || string.IsNullOrEmpty(baseTypeName))
        {
            return false;
        }

        if (classDeclaration.BaseList?.Types.Count > 0)
        {
            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                var type = baseType.Type.ToString();
                if (type == baseTypeName || type.EndsWith($".{baseTypeName}"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static WorkspaceSymbol? FindSymbolByName(
        Dictionary<string, WorkspaceSymbol> allProjectSymbols,
        string className,
        string? domainNamespace)
    {
        className = className.TrimEnd('?');

        if (!string.IsNullOrEmpty(domainNamespace))
        {
            foreach (var symbol in allProjectSymbols.Values)
            {
                if (symbol.Name == className && symbol.Namespace.StartsWith(domainNamespace))
                {
                    return symbol;
                }
            }
        }
        else
        {
            foreach (var symbol in allProjectSymbols.Values)
            {
                if (symbol.Name == className)
                {
                    return symbol;
                }
            }
        }

        return null;
    }

    private static PropertyMeta GetPropertyForValueObject(WorkspaceSymbol symbol, PropertyMeta propertyMeta)
    {
        FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

        var nullAble = propertyMeta.Type.EndsWith("?") ? "?" : string.Empty;

        if (publicPropertiesCollector.Properties.Count == 1)
        {
            return new PropertyMeta
            {
                Name = propertyMeta.Name,
                Type = publicPropertiesCollector.Properties.First().Type + nullAble,
                IsSimpleType = true,
                Order = propertyMeta.Order,
                IsSetPublic = propertyMeta.IsSetPublic,
                IsCollection = propertyMeta.IsCollection
            };
        }

        return new PropertyMeta
        {
            Name = propertyMeta.Name,
            Type = propertyMeta.Type + "Dto",
            IsSimpleType = propertyMeta.IsSimpleType,
            Order = propertyMeta.Order,
            IsSetPublic = propertyMeta.IsSetPublic,
            IsCollection = propertyMeta.IsCollection
        };
    }

    public AnalyzeSolutionService(
        ILogger<AnalyzeSolutionService> logger,
        IOptions<AppConfig> appConfig,
        IGenerateCodeService generateCodeService,
        CommandLineArgs commandLineArgs)
    {
        _logger = logger;
        _appConfig = appConfig;
        _generateCodeService = generateCodeService;
        _commandLineArgs = commandLineArgs;
    }

    public async Task AnalyzeSolution()
    {
        if (string.IsNullOrEmpty(_commandLineArgs.SolutionPath))
        {
            throw new ArgumentException("Solution path must be provided.");
        }

        var solution = await WorkspaceSolution.Load(_commandLineArgs.SolutionPath, s => _logger.LogInformation(s));

        _allProjectSymbols = await solution.AllProjectSymbols();

        var domainNamespace = _appConfig.Value.DomainNamespace;
        var className = _commandLineArgs.ClassName;

        var symbol = FindSymbolByName(_allProjectSymbols, className, domainNamespace);
        if (symbol != null)
        {
            _logger.LogInformation($"Found class: {symbol.Name} in namespace {symbol.Namespace}");

            FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
            publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);
            if (publicPropertiesCollector.Properties.Count > 0)
            {
                var properties = new List<PropertyMeta>();

                foreach (var prop in publicPropertiesCollector.Properties)
                {
                    if (prop.IsSimpleType)
                    {
                        properties.Add(prop);
                    }
                    else if (!prop.IsCollection)
                    {
                        var psymbol = FindSymbolByName(_allProjectSymbols, prop.Type, null);
                        if (psymbol != null)
                        {
                            if (InheritsFrom(psymbol.DeclarationSyntaxForClass, _appConfig.Value.ValueObjectClass))
                            {
                                properties.Add(GetPropertyForValueObject(psymbol, prop));
                            }
                            else
                            {
                                properties.Add(new PropertyMeta
                                {
                                    Name = prop.Name + "Dto",
                                    Type = prop.Type,
                                    IsSimpleType = false,
                                    Order = prop.Order,
                                    IsSetPublic = prop.IsSetPublic,
                                    IsCollection = prop.IsCollection
                                });
                            }
                        }
                    }
                }

                var classMeta = new ClassMeta
                {
                    Name = symbol.Name,
                    NameSpace = symbol.Namespace,
                    Properties = properties
                };

                await _generateCodeService.GenerateCode(classMeta, Path.GetDirectoryName(_commandLineArgs.SolutionPath));
            }
        }
        else
        {
            _logger.LogWarning($"Class {className} not found in namespace {domainNamespace}.");
        }
    }
}