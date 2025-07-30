using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.ServicesModel;
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

    private static WorkspaceSymbol? FindSymbolByName(
        Dictionary<string, WorkspaceSymbol> allProjectSymbols,
        string className,
        string? domainNamespace)
    {
        className = className.TrimEnd('?');

        if (!string.IsNullOrEmpty(domainNamespace))
        {
            domainNamespace += ".";

            if (allProjectSymbols.TryGetValue($"{domainNamespace}.{className}", out var foundSymbol))
            {
                return foundSymbol;
            }

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
            if (allProjectSymbols.TryGetValue(className, out var foundSymbol))
            {
                return foundSymbol;
            }

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

    private static List<WorkspaceSymbol> GetChildSymbolsToGenerate(
        WorkspaceSymbol symbol,
        AppConfig appConfig,
        Dictionary<string, int> uniqueCheck,
        Dictionary<string, WorkspaceSymbol> allProjectSymbols)
    {
        var symbols = new List<WorkspaceSymbol>();

        var publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

        if (publicPropertiesCollector.Properties.Count > 0)
        {
            var dictionaryBaseClass = appConfig.DictionaryBaseClass;
            var entityBaseClass = appConfig.EntityBaseClass;

            foreach (var prop in publicPropertiesCollector.Properties)
            {
                if (uniqueCheck.ContainsKey(prop.FullName))
                {
                    continue;
                }

                if (!prop.IsSimpleType)
                {
                    if (!prop.IsCollection)
                    {
                        var psymbol = FindSymbolByName(allProjectSymbols, prop.Type, null);
                        if (psymbol != null)
                        {
                            if (!SyntaxHelpers.IsClassInheritingFrom(psymbol, dictionaryBaseClass)
                                && SyntaxHelpers.IsClassInheritingFrom(psymbol, entityBaseClass))
                            {
                                symbols.Add(psymbol);
                            }
                        }
                    }
                    else if (prop.IsCollection)
                    {
                        var className = prop.UnderlyingGenericTypeName;
                        if (!string.IsNullOrEmpty(className))
                        {
                            var psymbol = FindSymbolByName(allProjectSymbols, className, null);
                            if (psymbol != null)
                            {
                                if (!SyntaxHelpers.IsClassInheritingFrom(psymbol, dictionaryBaseClass)
                                    && SyntaxHelpers.IsClassInheritingFrom(psymbol, entityBaseClass))
                                {
                                    symbols.Add(psymbol);
                                }
                            }
                        }
                    }
                }
            }
        }

        return symbols;
    }

    private async Task GenerateConfiguration(WorkspaceSolution solution, WorkspaceSymbol symbol, GenerateCodeServiceConfig config, Dictionary<string, int> uniqueCheck)
    {
        var builder = new ClassMetaBuilderForConfiguration(_allProjectSymbols, solution, _appConfig.Value);
        var classMeta = builder.BuildClassMeta(symbol);
        await _generateCodeService.GenerateCodeForConfiguration(classMeta, config);

        uniqueCheck.TryAdd(symbol.FullName, 0);

        var childSymbols = GetChildSymbolsToGenerate(symbol, _appConfig.Value, uniqueCheck, _allProjectSymbols);

        foreach (var psymbol in childSymbols)
        {
            await GenerateConfiguration(solution, psymbol, config, uniqueCheck);
        }
    }

    private async Task GenerateBaseContracts(WorkspaceSymbol symbol, GenerateCodeServiceConfig config, Dictionary<string, int> uniqueCheck)
    {
        var builder = new ClassMetaBuilderForBaseCommand(_allProjectSymbols, _appConfig.Value);
        var classMeta = builder.BuildClassMeta(symbol);
        await _generateCodeService.GenerateCodeForBaseCommand(classMeta, config);

        uniqueCheck.TryAdd(symbol.FullName, 0);

        var childSymbols = GetChildSymbolsToGenerate(symbol, _appConfig.Value, uniqueCheck, _allProjectSymbols);

        foreach (var psymbol in childSymbols)
        {
            await GenerateBaseContracts(psymbol, config, uniqueCheck);
        }
    }

    private async Task GenerateCreateContracts(WorkspaceSymbol symbol, GenerateCodeServiceConfig config, Dictionary<string, int> uniqueCheck)
    {
        var builder = new ClassMetaBuilderForUpdateCommand(_allProjectSymbols, _appConfig.Value, uniqueCheck);
        var classMeta = builder.BuildClassMeta(symbol);

        classMeta.Order = uniqueCheck.Count;

        await _generateCodeService.GenerateCodeForCreateCommand(classMeta, config);

        uniqueCheck.TryAdd(symbol.FullName, 0);

        var childSymbols = GetChildSymbolsToGenerate(symbol, _appConfig.Value, uniqueCheck, _allProjectSymbols);

        foreach (var psymbol in childSymbols)
        {
            await GenerateCreateContracts(psymbol, config, uniqueCheck);
        }
    }

    private async Task GenerateUpdateContracts(WorkspaceSymbol symbol, GenerateCodeServiceConfig config, Dictionary<string, int> uniqueCheck)
    {
        var builder = new ClassMetaBuilderForUpdateCommand(_allProjectSymbols, _appConfig.Value, uniqueCheck);
        var classMeta = builder.BuildClassMeta(symbol);

        classMeta.Order = uniqueCheck.Count;

        await _generateCodeService.GenerateCodeForUpdateCommand(classMeta, config);

        uniqueCheck.TryAdd(symbol.FullName, 0);

        var childSymbols = GetChildSymbolsToGenerate(symbol, _appConfig.Value, uniqueCheck, _allProjectSymbols);

        foreach (var psymbol in childSymbols)
        {
            await GenerateUpdateContracts(psymbol, config, uniqueCheck);
        }
    }

    private async Task GenerateContracts(WorkspaceSymbol symbol, GenerateCodeServiceConfig config)
    {
        await GenerateBaseContracts(symbol, config, new Dictionary<string, int>());
        await GenerateCreateContracts(symbol, config, new Dictionary<string, int>());
        await GenerateUpdateContracts(symbol, config, new Dictionary<string, int>());
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

        var solutionDirectory = Path.GetDirectoryName(_commandLineArgs.SolutionPath);

        if (string.IsNullOrEmpty(solutionDirectory))
        {
            throw new ArgumentException("Solution path not exists.");
        }

        var solution = await WorkspaceSolution.Load(_commandLineArgs.SolutionPath, s => _logger.LogInformation(s));

        _allProjectSymbols = await solution.AllProjectSymbols();

        var symbol = FindSymbolByName(_allProjectSymbols, _commandLineArgs.ClassName, _appConfig.Value.DomainNamespace);

        if (symbol != null)
        {
            _logger.LogInformation($"Found class: {symbol.Name} in namespace {symbol.Namespace}");
            {
                var config = new GenerateCodeServiceConfig(
                    solutionDirectory,
                    $"{_appConfig.Value.InfrastructureNamespace}.{symbol.Name}s",
                    $"{_appConfig.Value.CommandsNamespace}.{symbol.Name}s" );
                await GenerateConfiguration(solution, symbol, config, new Dictionary<string, int>());
            }
            {
                var config = new GenerateCodeServiceConfig(
                    solutionDirectory,
                    $"{_appConfig.Value.ContractsNamespace}.{symbol.Name}s.Commands",
                    $"{_appConfig.Value.CommandsNamespace}.{symbol.Name}s");
                await GenerateContracts(symbol, config);
            }
        }
        else
        {
            _logger.LogWarning($"Class {_commandLineArgs.ClassName} not found in namespace {_appConfig.Value.DomainNamespace}.");
        }
    }
}