using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebApiScaffolding.Models.WorkspaceModel;

public class WorkspaceProject
{
    private readonly Project _project;
    private readonly Compilation _compilation;

    public string ProjectName => Path.GetFileName(_project.FilePath);

    public string ProjectPath => Path.GetDirectoryName(_project.FilePath);

    public string DefaultNamespace => _project.DefaultNamespace;

    public async Task<List<ISymbol>> AllProjectSymbols()
    {
        List<ISymbol> ls = new List<ISymbol>();
        foreach (Document d in _project.Documents)
        {
            var m = await d.GetSemanticModelAsync();
            var root = await d.GetSyntaxRootAsync();

            if (root != null)
            {
                List<ClassDeclarationSyntax> lc = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                foreach (var c in lc)
                {
                    ISymbol s = m.GetDeclaredSymbol(c);
                    ls.Add(s);
                }

                List<EnumDeclarationSyntax> enc = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();
                foreach (var c in enc)
                {
                    ISymbol s = m.GetDeclaredSymbol(c);
                    ls.Add(s);
                }
            }
        }

        return ls;
    }

    public static async Task<WorkspaceProject> LoadFromSolution(Project project, Action<string> logAction)
    {
        return project != null
            ? await LoadProject(logAction, _ => Task.FromResult(project))
            : null;
    }

    private static async Task<WorkspaceProject> LoadProject(Action<string> logAction, Func<WorkspaceProgressBarProjectLoadStatus, Task<Project>> getProject)
    {
        var project = await getProject(new WorkspaceProgressBarProjectLoadStatus(logAction));
        var compilation = await project.GetCompilationAsync();

        compilation = compilation.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var output = Path.Combine(Path.GetDirectoryName(project.FilePath), "bin");

        if (Directory.Exists(output))
        {
            var files = Directory.GetFiles(output, "*.dll").ToList();
            foreach (var f in files)
            {
                logAction($"AddReference {f}");
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(f));
            }
        }

        return new WorkspaceProject(project, compilation);
    }

    private WorkspaceProject(Project project, Compilation compilation)
    {
        _project = project;
        _compilation = compilation;
    }
}