using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.ServicesModel;
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

    public async Task GenerateCodeForConfiguration(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesForConfiguration(metadata, config, _appConfig.Value);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, config.Namespace);

            var code = await _templateService.GeneratedCode("ConfigurationTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"{metadata.Name}Configuration.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForBaseCommand(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesForContracts(metadata, config, _appConfig.Value);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, config.Namespace);

            var code = await _templateService.GeneratedCode("BaseCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"{metadata.Name}BaseCommand.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();

            newMetadata.Namespaces.Add(config.Namespace);

            GeneratorContext generator = new GeneratorContext(newMetadata, config.CommandsNamespace);

            var code = await _templateService.GeneratedCode("BaseCommandValidatorTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^3], $"{newMetadata.Name}BaseCommandValidator.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForCreateCommand(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesForContracts(metadata, config, _appConfig.Value);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, config.Namespace +".Create");

            var code = await _templateService.GeneratedCode("CreateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, "Create", $"Create{metadata.Name}Command.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();

            newMetadata.Namespaces.Add(config.Namespace +".Create");

            GeneratorContext generator = new GeneratorContext(newMetadata, $"{config.CommandsNamespace}.Create");

            var code = await _templateService.GeneratedCode("CreateCommandValidatorTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^2], $"Create{newMetadata.Name}CommandValidator.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForUpdateCommand(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesForContracts(metadata, config, _appConfig.Value);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, config.Namespace + ".Update");

            var code = await _templateService.GeneratedCode("UpdateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, "Update", $"Update{metadata.Name}Command.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();

            newMetadata.Namespaces.Add(config.Namespace +".Update");

            GeneratorContext generator = new GeneratorContext(newMetadata, $"{config.CommandsNamespace}.Update");

            var code = await _templateService.GeneratedCode("UpdateCommandValidatorTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^1], $"Update{newMetadata.Name}CommandValidator.cs"), code, Encoding.UTF8);
        }
    }

    private void CreateDirectoriesForContracts(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var contractsPath = appConfig.ContractsPath;
        var commandsPath = appConfig.CommandsPath;
        List<string> paths = [
            contractsPath,
            $"{contractsPath}\\{metadata.Name}s",
            $"{contractsPath}\\{metadata.Name}s\\Commands",
            $"{contractsPath}\\{metadata.Name}s\\Create",
            $"{contractsPath}\\{metadata.Name}s\\Update",
            commandsPath,
            $"{commandsPath}\\{metadata.Name}s",
            $"{commandsPath}\\{metadata.Name}s\\Create",
            $"{commandsPath}\\{metadata.Name}s\\Update"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths.First(x=>x.EndsWith("Commands"));
    }

    private void CreateDirectoriesForConfiguration(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var infrastructurePath = appConfig.InfrastructurePath;
        List<string> paths = [
            infrastructurePath,
            $"{infrastructurePath}\\{metadata.Name}s"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths.Last();
    }

    private void CreateDirectorySafely(string solutionPath, List<string> paths)
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