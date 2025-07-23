using Microsoft.CodeAnalysis;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceSolution : WorkspaceBase
{
    private readonly Solution _solution;

    private readonly IDictionary<string, WorkspaceProject> _openProjects = new Dictionary<string, WorkspaceProject>();

    public static async Task<WorkspaceSolution> Load(string fileName)
    {
        await Console.Out.WriteLineAsync("Loading solution...");
        var workspace = BuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(fileName, new WorkspaceProgressBarProjectLoadStatus());

        return new WorkspaceSolution(solution);
    }

    private WorkspaceSolution(Solution solution)
    {
        _solution = solution;
    }

    public async Task<Dictionary<string, ISymbol>> AllProjectSymbols()
    {
        foreach (var p in _solution.Projects)
        {
            _openProjects[p.Name] = await WorkspaceProject.LoadFromSolution(p);
        }

        Dictionary<string, ISymbol> allSymbols = new Dictionary<string, ISymbol>();

        foreach (var p in _openProjects)
        {
            var sym = await p.Value.AllProjectSymbols();

            foreach (var item in sym)
            {
                var key = item.ToString();
                if (!allSymbols.TryAdd(key, item))
                {
                    await Console.Out.WriteLineAsync($"Duplicate: {key}");
                }
            }
        }

        return allSymbols;
    }
}