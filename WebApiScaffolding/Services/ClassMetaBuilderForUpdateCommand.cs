using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderForUpdateCommand : ClassMetaBuilderBase, IClassMetaBuilder
{
    private readonly Dictionary<string, int> _uniqueTypeCheck;

    public ClassMetaBuilderForUpdateCommand(Dictionary<string, WorkspaceSymbol> symbols, AppConfig conf, Dictionary<string, int> uniqueCheck) : base(symbols, conf)
    {
        _uniqueTypeCheck = uniqueCheck;
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
                    continue;
                }

                if (_uniqueTypeCheck.ContainsKey(prop.FullName))
                {
                    continue;
                }

                if (!prop.IsCollection)
                {
                    var psymbol = FindSymbolByName(prop.Type, null);
                    if (psymbol != null)
                    {
                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.DictionaryBaseClass))
                        {
                            continue;
                        }

                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.BaseIdClass))
                        {
                            continue;
                        }

                        properties.Add(new PropertyMeta
                        {
                            Name = prop.Name,
                            Type = prop.Type,
                            IsSimpleType = false,
                            Order = prop.Order,
                            IsCollection = false,
                            IsValueObject = false,
                            WithOne = string.Empty,
                            ForeignKey = string.Empty
                        });
                    }
                }
                else if (prop.IsCollection)
                {
                    var className = prop.UnderlyingGenericTypeName;
                    if (!string.IsNullOrEmpty(className))
                    {
                        var sp = SyntaxHelpers.SplitFullName(className);

                        properties.Add(new PropertyMeta
                        {
                            Name = prop.Name,
                            Type = sp.ClassName,
                            IsSimpleType = false,
                            Order = prop.Order,
                            IsCollection = true,
                            IsValueObject = false
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
        return classMeta;
    }
}