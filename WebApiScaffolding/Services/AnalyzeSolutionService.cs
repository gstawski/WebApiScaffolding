using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Models.WorkspaceModel;

namespace WebApiScaffolding.Services;

public class AnalyzeSolutionService : IAnalyzeSolutionService
{
    private readonly IOptions<AppConfig> _appConfig;
    private readonly CommandLineArgs _commandLineArgs;

    private Dictionary<string,ISymbol> _allProjectSymbols = new();

    public AnalyzeSolutionService(IOptions<AppConfig> appConfig, CommandLineArgs commandLineArgs)
    {
        _appConfig = appConfig;
        _commandLineArgs = commandLineArgs;
    }

    public async Task AnalyzeSolution()
    {
        if (string.IsNullOrEmpty(_commandLineArgs.SolutionPath))
        {
            throw new ArgumentException("Solution path must be provided.");
        }

        var solution = await WorkspaceSolution.Load(_commandLineArgs.SolutionPath);

        _allProjectSymbols = await solution.AllProjectSymbols();
    }
}