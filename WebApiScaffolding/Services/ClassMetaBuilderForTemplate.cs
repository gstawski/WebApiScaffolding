using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderForTemplate : ClassMetaBuilderBase, IClassMetaBuilder
{
    private readonly WorkspaceSolution _solution;
    private readonly string _domainNamespace;

    public ClassMetaBuilderForTemplate(
        Dictionary<string, WorkspaceSymbol> symbols,
        WorkspaceSolution solution,
        string domainNamespace,
        string valueObjectClass) : base(symbols, valueObjectClass)
    {
        _solution = solution;
        _domainNamespace = domainNamespace;
    }

    public ClassMeta BuildClassMeta(WorkspaceSymbol symbol)
    {
        if (symbol.DeclarationSyntaxForClass == null)
        {
            throw new ArgumentException("Declaration syntax for class is null", nameof(symbol));
        }

        FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

        var properties = new List<PropertyMeta>();

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
                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, ValueObjectClass))
                        {
                            var findResult = _solution.FindClassesInheritingFrom(psymbol.Symbol, _domainNamespace).ConfigureAwait(false).GetAwaiter().GetResult();

                            if (findResult.Count > 0)
                            {

                            }

                            //properties.Add(GetPropertyForValueObject(psymbol, prop));
                        }
                        else if (SyntaxHelpers.IsClassInheritingFrom(psymbol, "DictionaryEntity"))
                        {
                            properties.Add(new PropertyMeta
                            {
                                Name = prop.Name,
                                Type = prop.Type,
                                IsSimpleType = false,
                                Order = prop.Order,
                                IsSetPublic = prop.IsSetPublic,
                                IsCollection = prop.IsCollection,
                                IsValueObject = false,
                                WithOne = string.Empty,
                                ForeignKey = $"IdDict{prop.Name}"
                            });
                        }
                        else
                        {
                            properties.Add(new PropertyMeta
                            {
                                Name = prop.Name,
                                Type = prop.Type,
                                IsSimpleType = false,
                                Order = prop.Order,
                                IsSetPublic = prop.IsSetPublic,
                                IsCollection = prop.IsCollection,
                                IsValueObject = false,
                                WithOne = string.Empty,
                                ForeignKey = string.Empty
                            });
                        }
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

        return classMeta;
    }

    /*private PropertyMeta GetPropertyForValueObject(WorkspaceSymbol psymbol, SyntaxPropertyMeta prop)
    {

    }*/
}