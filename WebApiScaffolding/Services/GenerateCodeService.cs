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

            {
                var code = await GenerateCode(metadata, "RepositoryTemplate.tt", config.Namespace);
                await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"{metadata.Name}Repository.cs"), code, Encoding.UTF8);
            }
            {
                var code = await GenerateCode(metadata, "RepositoryInterfaceTemplate.tt", config.Namespace);
                await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^1], $"I{metadata.Name}Repository.cs"), code, Encoding.UTF8);
            }
        }
        {
            var code = await GenerateCode(metadata, "ConfigurationTemplate.tt", config.Namespace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"{metadata.Name}Configuration.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForCommandHandlers(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesFoCommandHandlers(metadata, config, _appConfig.Value);
        }
        {
            var code = await GenerateCode(metadata, "BaseCommandHandler.tt", config.CommandsNamespace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^3], $"{metadata.Name}BaseCommandHandler.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(_appConfig.Value.ContractsNamespace + $".{newMetadata.Name}s.Commands.Create");
            newMetadata.Namespaces.Add($"{_appConfig.Value.DomainNamespace}.{newMetadata.Name}s");
            var code = await GenerateCode(newMetadata, "CreateCommandHandler.tt", $"{config.CommandsNamespace}.Create");
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^2], $"Create{newMetadata.Name}CommandHandler.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(_appConfig.Value.ContractsNamespace + $".{newMetadata.Name}s.Commands.Update");
            newMetadata.Namespaces.Add($"{_appConfig.Value.DomainNamespace}.{newMetadata.Name}s");
            var code = await GenerateCode(newMetadata, "UpdateCommandHandler.tt", $"{config.CommandsNamespace}.Update");
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^1], $"Update{newMetadata.Name}CommandHandler.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.NameSpace = $"Application.Queries.{config.RootClassName}s";
            newMetadata.Namespaces.Add($"{_appConfig.Value.DomainNamespace}.{metadata.Name}s");
            newMetadata.Namespaces.Add(_appConfig.Value.ContractsNamespace + $".{newMetadata.Name}s.Queries");
            newMetadata.Namespaces.Add(_appConfig.Value.ContractsNamespace + $".{newMetadata.Name}s.Queries.Responses");

            var code = await GenerateCode(newMetadata, "GetCommandHandler.tt", newMetadata.NameSpace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![1], $"Get{newMetadata.Name}CommandHandler.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForBaseCommand(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesForContracts(metadata, config, _appConfig.Value);
        }
        {
            var code = await GenerateCode(metadata, "BaseCommandTemplate.tt", config.CommandsNamespace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"{metadata.Name}BaseCommand.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(config.Namespace);
            newMetadata.Namespaces.Add($"{_appConfig.Value.DomainNamespace}.{config.RootClassName}s");
            var code = await GenerateCode(newMetadata, "BaseCommandValidatorTemplate.tt", config.CommandsNamespace);
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
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(config.CommandsNamespace);
            var code = await GenerateCode(newMetadata, "CreateCommandTemplate.tt", config.Namespace +".Create");
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, "Create", $"Create{newMetadata.Name}Command.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.NameSpace = config.CommandsNamespace;
            newMetadata.Namespaces.Add(config.Namespace +".Create");
            var code = await GenerateCode(newMetadata, "CreateCommandValidatorTemplate.tt", $"{config.CommandsNamespace}.Create");
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
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(config.CommandsNamespace);
            var code = await GenerateCode(newMetadata, "UpdateCommandTemplate.tt", config.Namespace + ".Update");
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, "Update", $"Update{newMetadata.Name}Command.cs"), code, Encoding.UTF8);
        }
        {
            var newMetadata = metadata.Clone();
            newMetadata.Namespaces.Add(config.Namespace +".Update");
            var code = await GenerateCode(newMetadata, "UpdateCommandValidatorTemplate.tt", $"{config.CommandsNamespace}.Update");
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^1], $"Update{newMetadata.Name}CommandValidator.cs"), code, Encoding.UTF8);
        }
    }

    public async Task GenerateCodeForGetCommand(ClassMeta metadata, GenerateCodeServiceConfig config)
    {
        if (string.IsNullOrEmpty(config.MainPath))
        {
            CreateDirectoriesFoGetContracts(metadata, config, _appConfig.Value);

            {
                var code = await GenerateCode(metadata, "GetQueryTemplate.tt", config.Namespace);
                await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![^2], $"Get{metadata.Name}Query.cs"), code, Encoding.UTF8);
            }
            {
                var code = await GenerateCode(metadata, "WebApiControllerTemplate.tt", config.Namespace);
                await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.AllCreatedPaths![1], $"{metadata.Name}Controller.cs"), code, Encoding.UTF8);
            }
        }
        if (metadata.Properties.Count > 0)
        {
            metadata.Namespaces.AddRange(config.AdditionalNamespaces);
            var code = await GenerateCode(metadata, "GetResponseTemplate.tt", config.Namespace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"Get{metadata.Name}Response.cs"), code, Encoding.UTF8);
        }
        else
        {
            var code = await GenerateCode(metadata, "GetResponseDictionary.tt", config.Namespace);
            await File.WriteAllTextAsync(Path.Combine(config.SolutionPath, config.MainPath!, $"Get{metadata.Name}Response.cs"), code, Encoding.UTF8);
        }
    }


    private async Task<string> GenerateCode(ClassMeta metadata, string templateName, string nameSpace)
    {
        try
        {
            _logger.LogInfo($"Generating {templateName} code for {metadata.Name}");
            //_logger.LogInformation("Generating {TemplateName} code for {ClassName}", templateName, metadata.Name);
            GeneratorContext generator = new GeneratorContext(metadata, nameSpace);

            var code = await _templateService.GeneratedCode(templateName, generator);
            return code;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error generating code for {metadata.Name} using template {templateName}");
        }

        return string.Empty;
    }

    private void CreateDirectoriesFoCommandHandlers(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var commandsPath = appConfig.CommandsPath;
        var queriesPath = appConfig.QueriesPath;
        List<string> paths = [
            queriesPath,
            $"{queriesPath}\\{metadata.Name}s",
            commandsPath,
            $"{commandsPath}\\{metadata.Name}s",
            $"{commandsPath}\\{metadata.Name}s\\Create",
            $"{commandsPath}\\{metadata.Name}s\\Update"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths[^1];
    }

    private void CreateDirectoriesFoGetContracts(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var contractsPath = appConfig.ContractsPath;
        var apiPath = appConfig.WebApiPath;
        List<string> paths = [
            apiPath,
            $"{apiPath}\\Controllers",
            contractsPath,
            $"{contractsPath}\\{metadata.Name}s",
            $"{contractsPath}\\{metadata.Name}s\\Queries",
            $"{contractsPath}\\{metadata.Name}s\\Queries\\Responses"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths[^1];
    }

    private void CreateDirectoriesForContracts(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var contractsPath = appConfig.ContractsPath;
        var commandsPath = appConfig.CommandsPath;
        List<string> paths = [
            contractsPath,
            $"{contractsPath}\\{metadata.Name}s",
            $"{contractsPath}\\{metadata.Name}s\\Commands",
            $"{contractsPath}\\{metadata.Name}s\\Commands\\Create",
            $"{contractsPath}\\{metadata.Name}s\\Commands\\Update",
            commandsPath,
            $"{commandsPath}\\{metadata.Name}s",
            $"{commandsPath}\\{metadata.Name}s\\Create",
            $"{commandsPath}\\{metadata.Name}s\\Update"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths.First(x=>x.EndsWith("Commands", StringComparison.Ordinal));
    }

    private void CreateDirectoriesForConfiguration(ClassMeta metadata, GenerateCodeServiceConfig config, AppConfig appConfig)
    {
        var infrastructurePath = appConfig.InfrastructurePath;
        var domainPath = _appConfig.Value.DomainPath;
        List<string> paths = [
            infrastructurePath,
            $"{infrastructurePath}\\{metadata.Name}s",
            domainPath,
            $"{domainPath}\\{metadata.Name}s"
        ];

        CreateDirectorySafely(config.SolutionPath, paths);

        config.AllCreatedPaths = paths;
        config.MainPath = paths[1];
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
                _logger.LogError(ex, $"Error creating directory {fullPath}");
            }
        }
    }
}