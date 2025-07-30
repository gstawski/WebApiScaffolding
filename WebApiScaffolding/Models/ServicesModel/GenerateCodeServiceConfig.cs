namespace WebApiScaffolding.Models.ServicesModel;

public class GenerateCodeServiceConfig
{
    public string SolutionPath { get; init; }
    public string Namespace { get; init; }

    public string CommandsNamespace { get; init; }

    public string? MainPath { get; set; }

    public List<string>? AllCreatedPaths { get; set; }

    public GenerateCodeServiceConfig(string solutionPath, string nameSpace, string commandsNamespace)
    {
        SolutionPath = solutionPath;
        Namespace = nameSpace;
        CommandsNamespace = commandsNamespace;
    }
}