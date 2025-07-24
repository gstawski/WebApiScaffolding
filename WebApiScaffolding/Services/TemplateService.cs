using System.CodeDom.Compiler;
using System.Text;
using Mono.TextTemplating;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Templates;

namespace WebApiScaffolding.Services;

public class TemplateService : ITemplateService
{
    public async Task<string> GeneratedCode(string templateFilename, GeneratorContext context)
    {
        string path = Directory.GetCurrentDirectory();
        var template = Path.Combine(path, "Templates", templateFilename);
        var templateContent = await File.ReadAllTextAsync(template);

        var generator = new TemplateGenerator();

        generator.Imports.Add("System.Text.Json");
        generator.Refs.Add(typeof(TemplateService).Assembly.Location);

        var session = generator.GetOrCreateSession();
        session["context"] = context.ToString();

        ParsedTemplate parsed = generator.ParseTemplate(templateFilename, templateContent);

        TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);

        //settings.CompilerOptions = "-nullable:enable";
        settings.Debug = false;
        settings.Encoding = Encoding.UTF8;
        settings.Log = Console.Out;

        (string _, string generatedContent) = await generator.ProcessTemplateAsync(
            parsed,
            template,
            templateContent,
            string.Empty,
            settings);

        if (generator.Errors.HasErrors)
        {
            StringBuilder sb = new StringBuilder();
            foreach (CompilerError error in generator.Errors)
            {
                sb.AppendLine($"{error.Line} : {error.ErrorNumber} {error.ErrorText}");
            }

            return sb.ToString();
        }

        return generatedContent;
    }
}