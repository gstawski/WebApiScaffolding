using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderForConfiguration : ClassMetaBuilderBase, IClassMetaBuilder
{
    private readonly WorkspaceSolution _solution;

    private (string propNameForClass, string propNameForId) GetConstraints(string className, string masterClassName, string masterIdType)
    {
        var symbol = FindSymbolByName(className, null);

        if (symbol != null)
        {
            var publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
            publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

            var propForClass = publicPropertiesCollector.Properties
                .FirstOrDefault(p => p.Type.Equals(masterClassName, StringComparison.OrdinalIgnoreCase)
                                     || p.Type.Trim('?').Equals(masterClassName, StringComparison.OrdinalIgnoreCase));

            var propForId = publicPropertiesCollector.Properties
                .FirstOrDefault(p => p.Type.Equals(masterIdType, StringComparison.OrdinalIgnoreCase)
                                     || p.Type.Trim('?').Equals(masterIdType, StringComparison.OrdinalIgnoreCase));

           return (propForClass?.Name ?? string.Empty, propForId?.Name ?? string.Empty);
        }

        return (string.Empty, string.Empty);
    }

    public ClassMetaBuilderForConfiguration(
        Dictionary<string, WorkspaceSymbol> symbols,
        WorkspaceSolution solution,
        AppConfig conf) : base(symbols, conf)
    {
        _solution = solution;

    }

    public ClassMeta BuildClassMeta(WorkspaceSymbol symbol)
    {
        if (symbol.DeclarationSyntaxForClass == null)
        {
            throw new ArgumentException("Declaration syntax for class is null", nameof(symbol));
        }

        var idType = GetBaseType(symbol.Symbol);

        var publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

        var properties = new List<PropertyMeta>();
        var namespaces = new Dictionary<string, int>();

        if (publicPropertiesCollector.Properties.Count > 0)
        {
            foreach (var prop in publicPropertiesCollector.Properties)
            {
                if (prop.IsSimpleType)
                {
                    properties.Add(prop.ToPropertyMeta());
                }
                else if (!prop.IsCollection)
                {
                    var psymbol = FindSymbolByName(prop.Type, null);
                    if (psymbol != null)
                    {
                        namespaces.TryAdd(psymbol.Namespace, 0);

                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.ValueObjectClass))
                        {
                            var findResult = _solution.FindClassesInheritingFrom(psymbol.Symbol, Config.DomainNamespace).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (findResult.Count > 0)
                            {
                                var className = findResult.FirstOrDefault()?.ClassName ?? string.Empty;

                                properties.Add(new PropertyMeta
                                {
                                    Name = prop.Name,
                                    Type = prop.Type,
                                    IsSimpleType = false,
                                    Order = prop.Order,
                                    IsCollection = prop.IsCollection,
                                    IsValueObject = true,
                                    WithOne = className,
                                    ForeignKey = string.Empty
                                });
                            }
                        }
                        else if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.DictionaryBaseClass))
                        {
                            properties.Add(new PropertyMeta
                            {
                                Name = prop.Name,
                                Type = prop.Type,
                                IsSimpleType = false,
                                Order = prop.Order,
                                IsCollection = prop.IsCollection,
                                IsValueObject = false,
                                WithOne = prop.Name,
                                ForeignKey = $"IdDict{prop.Type.TrimEnd('?')}"
                            });
                        }
                        else
                        {
                            //Ignore
                        }
                    }
                }
                else
                {
                    var className = prop.UnderlyingGenericTypeName;
                    if (!string.IsNullOrEmpty(className))
                    {
                        var sp = SyntaxHelpers.SplitFullName(className);

                        namespaces.TryAdd(sp.Namespace, 0);

                        var (findPropForClass, findPropForId) = GetConstraints(sp.ClassName, symbol.Name, idType);

                        properties.Add(new PropertyMeta
                        {
                            Name = prop.Name,
                            Type = prop.Type,
                            IsSimpleType = false,
                            Order = prop.Order,
                            IsCollection = prop.IsCollection,
                            IsValueObject = false,
                            WithOne = findPropForClass,
                            ForeignKey = findPropForId
                        });
                    }
                }
            }
        }

        var classMeta = new ClassMeta
        {
            Name = symbol.Name,
            NameSpace = symbol.Namespace,
            Properties = properties,
            Namespaces = namespaces.Keys.ToList()
        };

        return classMeta;
    }
}