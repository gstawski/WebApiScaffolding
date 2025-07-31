using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.SyntaxWalkers;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderBase
{
    protected AppConfig Config { get; }

    private readonly Dictionary<string, WorkspaceSymbol> _symbols;

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

    protected ClassMetaBuilderBase(Dictionary<string, WorkspaceSymbol> symbols, AppConfig config)
    {
        Config = config;
        _symbols = symbols;
    }

    protected static string GetBaseType(ISymbol symbol)
    {
        if (symbol.Kind == SymbolKind.NamedType)
        {
            var namedTypeSymbol = (INamedTypeSymbol)symbol;
            if (namedTypeSymbol.BaseType == null)
            {
                return string.Empty;
            }

            var baseType = namedTypeSymbol.BaseType;

            if (baseType.IsGenericType)
            {
                foreach (var type in baseType.TypeArguments)
                {
                    return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                }
            }
        }

        return string.Empty;
    }

    protected static (string typeName, bool isSimple) GetBaseType(WorkspaceSymbol symbol, SyntaxPropertyMeta propertyMeta, Func<string, WorkspaceSymbol?> findSymbolByName)
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

        return (string.Empty, false);
    }

    protected WorkspaceSymbol? FindSymbolByName(string className)
    {
        return FindSymbolByName(className, null);
    }

    protected WorkspaceSymbol? FindSymbolByName(
        string className,
        string? domainNamespace)
    {
        className = className.TrimEnd('?');

        if (!string.IsNullOrEmpty(domainNamespace))
        {
            if (_symbols.TryGetValue($"{domainNamespace}.{className}", out var foundSymbol))
            {
                return foundSymbol;
            }

            foreach (var symbol in _symbols.Values)
            {
                if (symbol.Name == className && symbol.Namespace.StartsWith(domainNamespace))
                {
                    return symbol;
                }
            }
        }
        else
        {
            if (_symbols.TryGetValue(className, out var foundSymbol))
            {
                return foundSymbol;
            }

            foreach (var symbol in _symbols.Values)
            {
                if (symbol.Name == className)
                {
                    return symbol;
                }
            }
        }

        return null;
    }
}