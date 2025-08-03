using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderForGetCommand : ClassMetaBuilderBase, IClassMetaBuilder
{
    private readonly Dictionary<string, int> _uniqueCheck;

    public ClassMetaBuilderForGetCommand(Dictionary<string, WorkspaceSymbol> symbols, AppConfig config, Dictionary<string, int> uniqueCheck) : base(symbols, config)
    {
        _uniqueCheck = uniqueCheck;
    }

    public ClassMeta BuildClassMeta(WorkspaceSymbol symbol)
    {
        if (symbol.DeclarationSyntaxForClass == null)
        {
            throw new ArgumentException("Declaration syntax for class is null", nameof(symbol));
        }

        if (SyntaxHelpers.IsClassInheritingFrom(symbol, Config.DictionaryBaseClass))
        {
            return  new ClassMeta
            {
                Name = symbol.Name,
                NameSpace = symbol.Namespace
            };
        }

        FindPublicPropertiesCollector publicPropertiesCollector = new FindPublicPropertiesCollector(symbol.Model);
        publicPropertiesCollector.Visit(symbol.DeclarationSyntaxForClass);

        var properties = new List<PropertyMeta>();
        if (publicPropertiesCollector.Properties.Count > 0)
        {
            properties.Add(new PropertyMeta
            {
                Name = "Id",
                Type = "int",
                Order = 1,
                IsSimpleType = true,
                IsValueObject  = false
            });

            foreach (var prop in publicPropertiesCollector.Properties)
            {
                if (prop.IsSimpleType)
                {
                    continue;
                }

                if (_uniqueCheck.ContainsKey(prop.FullName))
                {
                    continue;
                }

                if (!prop.IsCollection)
                {
                    var psymbol = FindSymbolByName(prop.Type, null);
                    if (psymbol != null)
                    {
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
                    var classTypeName = prop.UnderlyingGenericTypeName;
                    if (!string.IsNullOrEmpty(classTypeName))
                    {
                        var sp = SyntaxHelpers.SplitFullName(classTypeName);

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