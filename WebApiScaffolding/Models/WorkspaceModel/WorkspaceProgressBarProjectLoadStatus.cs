using Microsoft.CodeAnalysis.MSBuild;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceProgressBarProjectLoadStatus : IProgress<ProjectLoadProgress>
{
    private readonly Action<string> _logAction;

    public WorkspaceProgressBarProjectLoadStatus(Action<string> logAction)
    {
        _logAction = logAction;
    }

    public void Report(ProjectLoadProgress value)
    {
        _logAction($"{value.Operation} {value.FilePath}");
    }
}