using WebApiScaffolding.Models.ServicesModel;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Interfaces;

public interface IGenerateCodeService
{
    Task GenerateCodeForCommandHandlers(ClassMeta metadata, GenerateCodeServiceConfig config);
    Task GenerateCodeForConfiguration(ClassMeta metadata, GenerateCodeServiceConfig config);
    Task GenerateCodeForBaseCommand(ClassMeta metadata, GenerateCodeServiceConfig config);
    Task GenerateCodeForCreateCommand(ClassMeta metadata, GenerateCodeServiceConfig config);
    Task GenerateCodeForUpdateCommand(ClassMeta metadata, GenerateCodeServiceConfig config);
    Task GenerateCodeForGetCommand(ClassMeta metadata, GenerateCodeServiceConfig config);
}