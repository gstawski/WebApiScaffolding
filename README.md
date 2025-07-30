Purpose of this project is automaticaly generate code base on domain class definition.

How it works:
- Read information about class and its propertis and fileds types (recursive, all code tree)
- Recursive code generation using templates
    BaseCommandHandler.tt - base class for update handlers
    BaseCommandTemplate.tt - base class for command class
    BaseCommandValidatorTemplate.tt - base clas for FluentValidation class
    ConfigurationTemplate.tt - class implementing IEntityTypeConfiguration for Microsoft.EntityFrameworkCore;
    CreateCommandHandler.tt - command handler creating new items
    CreateCommandTemplate.tt - transport class for creating new items
    CreateCommandValidatorTemplate.tt - validation for transport class for creating new items 
    UpdateCommandHandler.tt - command handler updating new items
    UpdateCommandTemplate.tt - transport class for updating new items
    UpdateCommandValidatorTemplate.t - validation for transport class for updating new items

Technologies used:
- NET 9.0
- Microsoft.CodeAnalysis - for extracting information about our class from project
- Mono.TextTemplating - generate code using T4 templates

Configuration:
- "DomainNamespace": "Domain",
- "InfrastructureNamespace": "Infrastructure",
- "CommandsNamespace" : "Application.Commands",
- "ContractsNamespace" : "Application.Contracts",
- "InfrastructurePath" : "src\\Infrastructure", - place where EntityFrameworkCore configuration class should be generated
- "ContractsPath" : "src\\Application.Contracts", - place where cotract class should be generated
- "CommandsPath" : "src\\Application.Commands", - place where validation and handlers should be generated
- "ValueObjectClass" : "ValueObject", - base class for ids
- "DictionaryBaseClass" : "DictionaryEntity", - base clas for dictionaries 
- "EntityBaseClass" : "Entity" - base clas for enties

How to execute app:
WebApiScaffolding.exe c:\Proj\MyWebApiApp\MyWebApiApp.sln UmpaPa
- c:\Proj\MyWebApiApp\MyWebApiApp.sln - path to solution
- UmpaPa - domain class to generate code
