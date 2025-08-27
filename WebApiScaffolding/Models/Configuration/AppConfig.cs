namespace WebApiScaffolding.Models.Configuration;

public class AppConfig
{
    public required string DomainNamespace { get; init; }
    public required string InfrastructureNamespace { get; init; }
    public required string CommandsNamespace { get; init; }
    public required string ContractsNamespace { get; init; }
    public required string InfrastructurePath { get; init; }
    public required string ContractsPath { get; init; }
    public required string CommandsPath { get; init; }
    public required string DomainPath { get; init; }
    public required string WebApiPath { get; init; }
    public required string QueriesPath { get; init; }
    public required string BaseIdClass { get; init; }
    public required string DictionaryBaseClass { get; init; }
    public required string EntityBaseClass { get; init; }
}