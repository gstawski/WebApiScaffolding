using Microsoft.CodeAnalysis.MSBuild;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceProgressBarProjectLoadStatus : IProgress<ProjectLoadProgress>
{
    public void Report(ProjectLoadProgress value)
    {
        Console.Out.WriteLine($"{value.Operation} {value.FilePath}");
    }
}