using WebApiScaffolding.Models.Templates;
using WebApiScaffolding.Models.WorkspaceModel;

namespace WebApiScaffolding.Interfaces;

public interface IClassMetaBuilder
{
    ClassMeta BuildClassMeta(WorkspaceSymbol symbol);
}