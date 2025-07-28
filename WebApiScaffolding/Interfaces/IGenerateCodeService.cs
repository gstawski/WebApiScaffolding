using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Interfaces;

public interface IGenerateCodeService
{
    Task GenerateCodeForCommands(ClassMeta metadata, string solutionPath);
}