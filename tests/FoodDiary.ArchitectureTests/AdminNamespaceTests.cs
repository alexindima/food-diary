using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AdminNamespaceTests {
    [Theory]
    [InlineData("Application")]
    [InlineData("Application.Abstractions")]
    [InlineData("Contracts")]
    [InlineData("Domain")]
    [InlineData("Infrastructure")]
    [InlineData("PersistenceModel")]
    [InlineData("Presentation")]
    [InlineData("tests/FoodDiary.Modules.Admin.Application.Tests")]
    [InlineData("tests/FoodDiary.Modules.Admin.Domain.Tests")]
    [InlineData("tests/FoodDiary.Modules.Admin.Infrastructure.Tests")]
    [InlineData("tests/FoodDiary.Modules.Admin.Infrastructure.IntegrationTests")]
    [InlineData("tests/FoodDiary.Modules.Admin.Presentation.Tests")]
    public void Projects_UseProjectNamesAndFolderNamespaces(string project) {
        string directory = ArchitectureTestPaths.FromRoot("Modules", "Admin", project);
        string projectFile = Assert.Single(Directory.EnumerateFiles(directory, "*.csproj"));
        string expectedRoot = project.StartsWith("tests/", StringComparison.Ordinal)
            ? project[6..] : "FoodDiary.Modules.Admin." + project;
        Assert.Equal(expectedRoot, Path.GetFileNameWithoutExtension(projectFile));
        Assert.Empty(XDocument.Load(projectFile).Descendants("RootNamespace"));
        string[] files = ModuleSourceCatalog.RequiredFiles(directory);
        var violations = new List<string>();
        foreach (string file in files) {
            string relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            string folder = Path.GetDirectoryName(relative)?.Replace('\\', '.').Replace('/', '.') ?? string.Empty;
            string expected = expectedRoot + (folder.Length == 0 ? string.Empty : "." + folder);
            foreach (string actual in ReadDeclaredNamespaces(File.ReadAllText(file))) {
                if (!string.Equals(actual, expected, StringComparison.Ordinal)) {
                    violations.Add($"{project}/{relative}: '{actual}', expected '{expected}'.");
                }
            }
        }
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Theory]
    [InlineData("public class Example { }", false)]
    [InlineData("public record Example;", false)]
    [InlineData("public delegate void Example();", false)]
    [InlineData("namespace Expected; public class Example { }", true)]
    [InlineData("namespace Expected { public class Example { } }", true)]
    [InlineData("namespace Wrong; public class Example { }", false)]
    [InlineData("namespace Expected { public class Example { } } public class Global { }", false)]
    public void NamespaceDetection_RequiresTypesToHaveTheExpectedNamespace(string source, bool expectedValid) {
        string[] actual = [.. ReadDeclaredNamespaces(source)];
        Assert.NotEmpty(actual);
        Assert.Equal(expectedValid, actual.All(value => string.Equals(value, "Expected", StringComparison.Ordinal)));
    }

    private static IEnumerable<string> ReadDeclaredNamespaces(string source) {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        foreach (BaseNamespaceDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()) {
            yield return string.Join('.', declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse().Select(parent => parent.Name.ToString()).Append(declaration.Name.ToString()));
        }
        foreach (MemberDeclarationSyntax declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                     .Where(declaration => declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)) {
            if (!declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any()) {
                yield return "<global namespace>";
            }
        }
    }
}
