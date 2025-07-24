using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApiScaffolding.Interfaces;
using WebApiScaffolding.Models.Configuration;
using WebApiScaffolding.Services;

namespace WebApiScaffolding;

internal class Program
{
    private static void RegisterLocator()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
        }

        Console.WriteLine("MSBuildLocator.RegisterInstance done!");
    }

    private static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Hello World!");

            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the solution path as an argument.");

                return;
            }

            if (args.Length == 1)
            {
                Console.WriteLine("Please provide the class name as an argument.");

                return;
            }

            RegisterLocator();

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService<AppWorker>();

            builder.Services.AddSingleton(new CommandLineArgs
            {
                SolutionPath = args.Length > 0 ? args[0] : string.Empty,
                ClassName = args.Length > 1 ? args[1] : string.Empty
            });

            builder.Services.AddSingleton<IAnalyzeSolutionService, AnalyzeSolutionService>();
            builder.Services.AddSingleton<ITemplateService, TemplateService>();
            builder.Services.AddSingleton<IGenerateCodeService, GenerateCodeService>();

            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            });

            builder.Services.Configure<AppConfig>(
                builder.Configuration.GetSection("AppConfig")
            );

            var host = builder.Build();

            await host.RunAsync();

            Console.WriteLine("All done!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}