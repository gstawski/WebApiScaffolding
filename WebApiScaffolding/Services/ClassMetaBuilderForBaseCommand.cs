using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.SyntaxWalkers;
using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;
using WebApiScaffolding.SyntaxWalkers;

namespace WebApiScaffolding.Services;

public class ClassMetaBuilderForBaseCommand : ClassMetaBuilderBase, IClassMetaBuilder
{
    public ClassMetaBuilderForBaseCommand(Dictionary<string, WorkspaceSymbol> symbols, AppConfig conf) : base(symbols, conf)
    {
    }

    private static PropertyMeta GetPropertyForValueObject(WorkspaceSymbol symbol, SyntaxPropertyMeta propertyMeta, Func<string, WorkspaceSymbol?> findSymbolByName)
    {
        var (typeName, isSimple) = GetBaseType(symbol, propertyMeta, findSymbolByName);

        if (string.IsNullOrEmpty(typeName))
        {
            isSimple = propertyMeta.IsSimpleType;
            if (propertyMeta.Type.EndsWith("?"))
            {
                typeName = propertyMeta.Type.Trim('?') + "Dto?";
            }
            else
            {
                typeName = propertyMeta.Type + "Dto";
            }
        }

        return new PropertyMeta
        {
            Name = propertyMeta.Name,
            Type = typeName,
            IsSimpleType = isSimple,
            Order = propertyMeta.Order,
            IsCollection = propertyMeta.IsCollection,
            IsValueObject = true
        };
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
                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.DictionaryBaseClass))
                        {
                            continue;
                        }

                        if (SyntaxHelpers.IsClassInheritingFrom(psymbol, Config.BaseIdClass))
                        {
                            properties.Add(GetPropertyForValueObject(psymbol, prop, FindSymbolByName));
                        }
                    }
                }
            }
        }

        var classMeta = new ClassMeta
        {
            Name = symbol.Name,
            NameSpace = symbol.Namespace,
            Properties = properties,
        };
        return classMeta;
    }
}