using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Interfaces;

public interface IGenerateCodeService
{
    Task GenerateCode(ClassMeta metadata, string solutionPath);
}