using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Services;

public class GenerateCodeService : IGenerateCodeService
{
    private readonly ILogger<GenerateCodeService> _logger;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly ITemplateService _templateService;

    public GenerateCodeService(
        ILogger<GenerateCodeService> logger,
        IOptions<AppConfig> appConfig,
        ITemplateService templateService)
    {
        _logger = logger;
        _appConfig = appConfig;
        _templateService = templateService;
    }

    public async Task GenerateCodeForCommands(ClassMeta metadata, string solutionPath)
    {
        var path = CreateDirectoriesForContracts(metadata, solutionPath);

        string lastCatalogName = Path.GetFileName(_appConfig.Value.ContractsPath);

        {
            GeneratorContext generator = new GeneratorContext(metadata, $"{lastCatalogName}.{metadata.Name}s.Commands");

            var code = await _templateService.GeneratedCode("CreateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, $"Create{metadata.Name}Command.cs"), code);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, $"{lastCatalogName}.{metadata.Name}s.Commands");

            var code = await _templateService.GeneratedCode("UpdateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, $"Update{metadata.Name}Command.cs"), code);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, $"{lastCatalogName}.{metadata.Name}s.Commands");

            var code = await _templateService.GeneratedCode("BaseCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, $"{metadata.Name}BaseCommand.cs"), code);
        }
    }

    private string CreateDirectoriesForContracts(ClassMeta metadata, string solutionPath)
    {
        var contracts = _appConfig.Value.ContractsPath;
        string[] paths = {
            contracts,
            $"{contracts}/{metadata.Name}s",
            $"{contracts}/{metadata.Name}s/Commands"
        };

        CreateDirectorySafely(solutionPath, paths);

        return paths.Last();
    }

    private void CreateDirectorySafely(string solutionPath, string[] paths)
    {
        foreach (var path in paths)
        {
            string fullPath = Path.Combine(solutionPath, path);
            try
            {
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating directory {DirectoryPath}", fullPath);
            }
        }
    }
}