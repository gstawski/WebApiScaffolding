using WebApiScaffolding.Models.WorkspaceModel;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderBase
{
    protected string ValueObjectClass { get; }
    private readonly Dictionary<string, WorkspaceSymbol> _symbols;

    protected ClassMetaBuilderBase(Dictionary<string, WorkspaceSymbol> symbols, string valueObjectClass)
    {
        ValueObjectClass = valueObjectClass;
        _symbols = symbols;
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