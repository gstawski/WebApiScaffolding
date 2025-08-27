namespace WebApiScaffolding.Models.Configuration;

public class AppConfig
{
    public string DomainNamespace { get; init; }
    public string InfrastructureNamespace { get; init; }
    public string CommandsNamespace { get; init; }
    public string ContractsNamespace { get; init; }
    public string InfrastructurePath { get; init; }
    public string ContractsPath { get; init; }
    public string CommandsPath { get; init; }
    public string DomainPath { get; init; }
    public string WebApiPath { get; init; }
    public string QueriesPath { get; init; }
    public string BaseIdClass { get; init; }
    public string DictionaryBaseClass { get; init; }
    public string EntityBaseClass { get; init; }
}