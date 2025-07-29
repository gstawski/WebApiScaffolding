using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Interfaces;

public interface IGenerateCodeService
{
    Task<string> GenerateCodeForConfiguration(ClassMeta metadata, string? filePath, Dictionary<string,string> config);
    Task<string> GenerateCodeForBaseCommand(ClassMeta metadata, string? filePath, Dictionary<string, string> config);
    Task<string> GenerateCodeForCreateCommand(ClassMeta metadata, string? filePath, Dictionary<string, string> config);
    Task<string> GenerateCodeForUpdateCommand(ClassMeta metadata, string? filePath, Dictionary<string, string> config);
}