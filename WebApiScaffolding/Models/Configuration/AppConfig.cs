namespace WebApiScaffolding.Models.Configuration;

public class AppConfig
{
    public string DomainNamespace { get; set; }
    public string InfrastructureNamespace { get; set; }
    public string CommandsNamespace { get; set; }
    public string ContractsNamespace { get; set; }
    public string InfrastructurePath { get; set; }
    public string ContractsPath { get; set; }
    public string ValueObjectClass { get; set; }
    public string DictionaryBaseClass { get; set; }
    public string EntityBaseClass { get; set; }
}