using Microsoft.Extensions.Logging;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Services;

public class GenerateCodeService : IGenerateCodeService
{
    private readonly ILogger<GenerateCodeService> _logger;
    private readonly ITemplateService _templateService;

    public GenerateCodeService(
        ILogger<GenerateCodeService> logger,
        ITemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    public async Task GenerateCode(ClassMeta metadata)
    {
        GeneratorContext g1 = new GeneratorContext(metadata, "WebApiScaffolding.Model");

        var code = await _templateService.GeneratedCode("FirstTest.tt", g1);

        _logger.LogInformation("Generated code: {Code}", code);
    }
}