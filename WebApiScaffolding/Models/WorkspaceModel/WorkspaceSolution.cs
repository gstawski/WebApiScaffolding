using Microsoft.CodeAnalysis;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceSolution : WorkspaceBase
{
    private readonly Solution _solution;
    private readonly Action<string> _logAction;

    private readonly IDictionary<string, WorkspaceProject> _openProjects = new Dictionary<string, WorkspaceProject>();

    public static async Task<WorkspaceSolution> Load(string fileName, Action<string> logAction)
    {
        logAction("Loading solution...");
        var workspace = BuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(fileName, new WorkspaceProgressBarProjectLoadStatus(logAction));

        return new WorkspaceSolution(solution, logAction);
    }

    private WorkspaceSolution(Solution solution, Action<string> logAction)
    {
        _solution = solution;
        _logAction = logAction;
    }

    public async Task<Dictionary<string, ISymbol>> AllProjectSymbols()
    {
        foreach (var p in _solution.Projects)
        {
            _openProjects[p.Name] = await WorkspaceProject.LoadFromSolution(p, _logAction);
        }

        Dictionary<string, ISymbol> allSymbols = new Dictionary<string, ISymbol>();

        foreach (var p in _openProjects)
        {
            var sym = await p.Value.AllProjectSymbols();

            foreach (var item in sym)
            {
                var key = $"{item.ContainingNamespace}.{item}";
                if (!allSymbols.TryAdd(key, item))
                {
                    _logAction($"Duplicate: {key}");
                }
            }
        }

        return allSymbols;
    }
}