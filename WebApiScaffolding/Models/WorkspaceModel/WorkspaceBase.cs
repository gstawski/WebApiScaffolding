using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace WebApiScaffolding.Models.WorkspaceModel;

public abstract class WorkspaceBase
{
    protected static MSBuildWorkspace BuildWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();
        workspace.SkipUnrecognizedProjects = true;
        return ConfigureWorkspace(workspace);
    }

    private static T ConfigureWorkspace<T>(T workspace)
        where T : Workspace
    {
        workspace.WorkspaceFailed += (_, args) =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                Console.Error.WriteLine(args.Diagnostic.Message);
            }
        };
        return workspace;
    }
}