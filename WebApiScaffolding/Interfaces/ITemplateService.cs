using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Interfaces;

public interface ITemplateService
{
    Task<string> GeneratedCode(string templateFilename, GeneratorContext context);
}