using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.SyntaxWalkers;
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

    private static bool IsPrimitiveType(ITypeSymbol typeSymbol)
    {
        switch (typeSymbol.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
            case SpecialType.System_Char:
            case SpecialType.System_Double:
            case SpecialType.System_Single:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
            default:
                return false;
        }
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

    private static List<IPropertySymbol> GetPropertiesSetByPrimaryConstructor(SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        var results = new List<IPropertySymbol>();

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null)
        {
            return results;
        }

        var primaryBaseType = classDeclaration.BaseList?.Types
            .OfType<PrimaryConstructorBaseTypeSyntax>()
            .FirstOrDefault();

        if (primaryBaseType != null)
        {
            var symbol = findSymbolByName(primaryBaseType.Type.ToString());

            if (symbol == null)
            {
                return results;
            }

            var currentType = symbol.Symbol;
            while (currentType != null)
            {
                var valueProperty = currentType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.GetMethod != null);

                if (valueProperty != null)
                {
                    results.Add(valueProperty);
                    break;
                }

                currentType = currentType.BaseType;
            }
        }

        return results;
    }

    private static (string typeName, bool isSimple) GetBaseType(WorkspaceSymbol symbol, SyntaxPropertyMeta propertyMeta, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        if (symbol.DeclarationSyntaxForClass == null)
        {
            return (propertyMeta.Type, propertyMeta.IsSimpleType);
        }

        var publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);
        if (publicPropertiesCollector.Properties.Count == 1)
        {
            var nullAble = propertyMeta.Type.EndsWith("?") ? "?" : string.Empty;
            return (publicPropertiesCollector.Properties.First().Type + nullAble, true);
        }

        var constructorCollector = new FindConstructorCollector(symbol.Model);
        constructorCollector.Visit(symbol.DeclarationSyntaxForClass);
        if (constructorCollector.Constructors.Count > 0)
        {
            var prop1 = constructorCollector.Constructors.First().GetPropertiesSetInConstructor(findSymbolByName);
            if (prop1.Count == 1)
            {
                var fproperty1 = prop1.First();
                return (fproperty1.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), IsPrimitiveType(fproperty1.Type));
            }

            var prop2 = GetPropertiesSetByPrimaryConstructor(symbol.Model, symbol.DeclarationSyntaxForClass, findSymbolByName);
            if (prop2.Count == 1)
            {
                var fproperty2 = prop2.First();
                return (fproperty2.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), IsPrimitiveType(fproperty2.Type));
            }
        }

        if (propertyMeta.Type.EndsWith("?"))
        {
            return (propertyMeta.Type.Trim('?') + "Dto?", propertyMeta.IsSimpleType);
        }

        return (propertyMeta.Type + "Dto", propertyMeta.IsSimpleType);
    }

    private static PropertyMeta GetPropertyForValueObject(WorkspaceSymbol symbol, SyntaxPropertyMeta propertyMeta, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        var (typeName, isSimple) = GetBaseType(symbol, propertyMeta, findSymbolByName);

        return new PropertyMeta
        {
            Name = propertyMeta.Name,
            Type = typeName,
            IsSimpleType = isSimple,
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

        var configurationSymbol = FindSymbolByName(_allProjectSymbols, $"{_commandLineArgs.ClassName}Configuration", _appConfig.Value.InfrastructureNamespace);

        var symbol = FindSymbolByName(_allProjectSymbols, _commandLineArgs.ClassName, _appConfig.Value.DomainNamespace);

        if (symbol != null)
        {
            _logger.LogInformation($"Found class: {symbol.Name} in namespace {symbol.Namespace}");

            FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
            publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);
            if (publicPropertiesCollector.Properties.Count > 0)
            {
                var properties = new List<PropertyMeta>();

                var navigationProperties = SyntaxHelpers.GetPropertiesWithNavigation(configurationSymbol);

                foreach (var prop in publicPropertiesCollector.Properties)
                {
                    if (navigationProperties.ContainsKey(prop.Name))
                    {
                        continue;
                    }

                    if (prop.IsSimpleType)
                    {
                        properties.Add(prop.ToPropertyMeta());
                    }
                    else if (!prop.IsCollection)
                    {
                        var psymbol = FindSymbolByName(_allProjectSymbols, prop.Type, null);
                        if (psymbol != null)
                        {
                            if (SyntaxHelpers.IsClassInheritingFrom(psymbol, _appConfig.Value.ValueObjectClass))
                            {
                                properties.Add(GetPropertyForValueObject(psymbol, prop, (classname) => FindSymbolByName(_allProjectSymbols, classname, null)));
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
            _logger.LogWarning($"Class {_commandLineArgs.ClassName} not found in namespace {_appConfig.Value.DomainNamespace}.");
        }
    }
}