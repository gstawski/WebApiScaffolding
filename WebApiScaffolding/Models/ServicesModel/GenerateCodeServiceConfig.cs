namespace WebApiScaffolding.Models.ServicesModel;

public class GenerateCodeServiceConfig
{
    public string SolutionPath { get; }
    public string Namespace { get; }

    public List<string> AdditionalNamespaces { get; } = new();

    public string CommandsNamespace { get; }

    public string RootClassName { get; }

    public string? MainPath { get; set; }

    public List<string>? AllCreatedPaths { get; set; }

    public GenerateCodeServiceConfig(string solutionPath, string nameSpace, string commandsNamespace, string rootClassName)
    {
        SolutionPath = solutionPath;
        Namespace = nameSpace;
        CommandsNamespace = commandsNamespace;
        RootClassName = rootClassName;
    }
}