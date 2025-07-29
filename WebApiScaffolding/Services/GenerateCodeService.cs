using System.Text;
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

    public async Task<string> GenerateCodeForConfiguration(ClassMeta metadata, string? filePath, Dictionary<string,string> config)
    {
        var solutionPath = config[ConfigKeys.SolutionPath];
        var path = filePath ?? CreateDirectoriesForConfiguration(metadata, solutionPath);
        {
            GeneratorContext generator = new GeneratorContext(metadata, config[ConfigKeys.NameSpace]);

            var code = await _templateService.GeneratedCode("ConfigurationTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, $"{metadata.Name}Configuration.cs"), code, Encoding.UTF8);
        }

        return path;
    }

    public async Task<string> GenerateCodeForBaseCommand(ClassMeta metadata, string? filePath, Dictionary<string,string> config)
    {
        var solutionPath = config[ConfigKeys.SolutionPath];
        var nameSpace = config[ConfigKeys.NameSpace];
        var path = filePath ?? CreateDirectoriesForContracts(metadata, solutionPath);
        {
            GeneratorContext generator = new GeneratorContext(metadata, nameSpace);

            var code = await _templateService.GeneratedCode("BaseCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, $"{metadata.Name}BaseCommand.cs"), code, Encoding.UTF8);
        }
        return path;
    }

    public async Task<string> GenerateCodeForCreateCommand(ClassMeta metadata, string? filePath, Dictionary<string,string> config)
    {
        var solutionPath = config[ConfigKeys.SolutionPath];
        var nameSpace = config[ConfigKeys.NameSpace];
        var path = filePath ?? CreateDirectoriesForContracts(metadata, solutionPath);
        {
            GeneratorContext generator = new GeneratorContext(metadata, nameSpace +".Create");

            var code = await _templateService.GeneratedCode("CreateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, "Create", $"Create{metadata.Name}Command.cs"), code, Encoding.UTF8);
        }
        {
            GeneratorContext generator = new GeneratorContext(metadata, nameSpace + ".Update");

            var code = await _templateService.GeneratedCode("UpdateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, "Update", $"Update{metadata.Name}Command.cs"), code, Encoding.UTF8);
        }

        return path;
    }

    public async Task<string> GenerateCodeForUpdateCommand(ClassMeta metadata, string? filePath, Dictionary<string,string> config)
    {
        var solutionPath = config[ConfigKeys.SolutionPath];
        var nameSpace = config[ConfigKeys.NameSpace];
        var path = filePath ?? CreateDirectoriesForContracts(metadata, solutionPath);
        {
            GeneratorContext generator = new GeneratorContext(metadata, nameSpace + ".Update");

            var code = await _templateService.GeneratedCode("UpdateCommandTemplate.tt", generator);

            await File.WriteAllTextAsync(Path.Combine(solutionPath, path, "Update", $"Update{metadata.Name}Command.cs"), code, Encoding.UTF8);
        }

        return path;
    }

    private string CreateDirectoriesForContracts(ClassMeta metadata, string solutionPath)
    {
        var directory = _appConfig.Value.ContractsPath;
        string[] paths = {
            directory,
            $"{directory}\\{metadata.Name}s",
            $"{directory}\\{metadata.Name}s\\Commands",
            $"{directory}\\{metadata.Name}s\\Create",
            $"{directory}\\{metadata.Name}s\\Update"
        };

        CreateDirectorySafely(solutionPath, paths);

        return paths.First(x=>x.EndsWith("Commands"));
    }

    private string CreateDirectoriesForConfiguration(ClassMeta metadata, string solutionPath)
    {
        var directory = _appConfig.Value.InfrastructurePath;
        string[] paths = {
            directory,
            $"{directory}\\{metadata.Name}s"
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