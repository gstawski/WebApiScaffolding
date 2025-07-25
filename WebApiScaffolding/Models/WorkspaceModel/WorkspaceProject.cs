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

    public async Task<List<WorkspaceSymbol>> AllProjectSymbols()
    {
        List<WorkspaceSymbol> ls = new List<WorkspaceSymbol>();
        foreach (Document d in _project.Documents)
        {
            var root = await d.GetSyntaxRootAsync();

            if (root != null)
            {
                List<ClassDeclarationSyntax> lc = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                foreach (var c in lc)
                {
                    var semanticModel = _compilation.GetSemanticModel(c.SyntaxTree);
                    var classSymbol = semanticModel.GetDeclaredSymbol(c);

                    if (classSymbol != null)
                    {
                        var workspaceSymbol = new WorkspaceSymbol
                        {
                            Model = semanticModel,
                            Symbol = classSymbol,
                            DeclarationSyntaxForClass = c,
                            DeclarationSyntaxForEnum = null
                        };
                        ls.Add(workspaceSymbol);
                    }
                }

                List<EnumDeclarationSyntax> enc = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();
                foreach (var c in enc)
                {
                    var semanticModel = _compilation.GetSemanticModel(c.SyntaxTree);
                    var enumSymbol = semanticModel.GetDeclaredSymbol(c);

                    if (enumSymbol != null)
                    {
                        var workspaceSymbol = new WorkspaceSymbol
                        {
                            Model = semanticModel,
                            Symbol = enumSymbol,
                            DeclarationSyntaxForClass = null,
                            DeclarationSyntaxForEnum = c
                        };
                        ls.Add(workspaceSymbol);
                    }
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