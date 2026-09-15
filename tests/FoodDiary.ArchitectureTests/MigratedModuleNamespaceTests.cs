using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MigratedModuleNamespaceTests {
    [Theory]
    [InlineData("Billing", "Application")]
    [InlineData("Billing", "Application.Abstractions")]
    [InlineData("Billing", "Contracts")]
    [InlineData("Billing", "Domain")]
    [InlineData("Billing", "Domain.Contracts")]
    [InlineData("Billing", "Infrastructure")]
    [InlineData("Billing", "PersistenceModel")]
    [InlineData("Billing", "Presentation")]
    [InlineData("Billing", "tests/FoodDiary.Modules.Billing.Application.Tests")]
    [InlineData("Billing", "tests/FoodDiary.Modules.Billing.Domain.Tests")]
    [InlineData("Billing", "tests/FoodDiary.Modules.Billing.Infrastructure.Tests")]
    [InlineData("Billing", "tests/FoodDiary.Modules.Billing.Presentation.Tests")]
    [InlineData("BodyMetrics", "Application")]
    [InlineData("BodyMetrics", "Application.Abstractions")]
    [InlineData("BodyMetrics", "Contracts")]
    [InlineData("BodyMetrics", "Domain")]
    [InlineData("BodyMetrics", "Infrastructure")]
    [InlineData("BodyMetrics", "PersistenceModel")]
    [InlineData("BodyMetrics", "Presentation")]
    [InlineData("BodyMetrics", "Presentation.Contracts")]
    [InlineData("BodyMetrics", "Presentation.Mappings")]
    [InlineData("BodyMetrics", "tests/FoodDiary.Modules.BodyMetrics.Application.Tests")]
    [InlineData("BodyMetrics", "tests/FoodDiary.Modules.BodyMetrics.Domain.Tests")]
    [InlineData("BodyMetrics", "tests/FoodDiary.Modules.BodyMetrics.Presentation.Tests")]
    [InlineData("ContentReports", "Application")]
    [InlineData("ContentReports", "Application.Abstractions")]
    [InlineData("ContentReports", "Contracts")]
    [InlineData("ContentReports", "Domain")]
    [InlineData("ContentReports", "Domain.Contracts")]
    [InlineData("ContentReports", "Infrastructure")]
    [InlineData("ContentReports", "PersistenceModel")]
    [InlineData("ContentReports", "Presentation")]
    [InlineData("ContentReports", "tests/FoodDiary.Modules.ContentReports.Application.Tests")]
    [InlineData("ContentReports", "tests/FoodDiary.Modules.ContentReports.Domain.Tests")]
    [InlineData("ContentReports", "tests/FoodDiary.Modules.ContentReports.Infrastructure.Tests")]
    [InlineData("ContentReports", "tests/FoodDiary.Modules.ContentReports.Presentation.Tests")]
    [InlineData("Cycles", "Application")]
    [InlineData("Cycles", "Application.Abstractions")]
    [InlineData("Cycles", "Contracts")]
    [InlineData("Cycles", "Domain")]
    [InlineData("Cycles", "Domain.Contracts")]
    [InlineData("Cycles", "Infrastructure")]
    [InlineData("Cycles", "PersistenceModel")]
    [InlineData("Cycles", "Presentation")]
    [InlineData("Cycles", "Presentation.Contracts")]
    [InlineData("Cycles", "Presentation.Mappings")]
    [InlineData("Cycles", "tests/FoodDiary.Modules.Cycles.Application.Tests")]
    [InlineData("Cycles", "tests/FoodDiary.Modules.Cycles.Domain.Tests")]
    [InlineData("Cycles", "tests/FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests")]
    [InlineData("Cycles", "tests/FoodDiary.Modules.Cycles.Infrastructure.Tests")]
    [InlineData("Cycles", "tests/FoodDiary.Modules.Cycles.Presentation.Tests")]
    [InlineData("Identity", "Infrastructure")]
    [InlineData("Identity", "PersistenceModel")]
    [InlineData("Identity", "Application.Abstractions")]
    [InlineData("DailyAdvices", "Application")]
    [InlineData("DailyAdvices", "Application.Abstractions")]
    [InlineData("DailyAdvices", "Contracts")]
    [InlineData("DailyAdvices", "Domain")]
    [InlineData("DailyAdvices", "Infrastructure")]
    [InlineData("DailyAdvices", "PersistenceModel")]
    [InlineData("DailyAdvices", "tests/FoodDiary.Modules.DailyAdvices.Application.Tests")]
    [InlineData("DailyAdvices", "tests/FoodDiary.Modules.DailyAdvices.Domain.Tests")]
    [InlineData("DailyAdvices", "tests/FoodDiary.Modules.DailyAdvices.Infrastructure.Tests")]
    [InlineData("Dashboard", "Application")]
    [InlineData("Dashboard", "Application.Abstractions")]
    [InlineData("Dashboard", "Contracts")]
    [InlineData("Dashboard", "Infrastructure")]
    [InlineData("Dashboard", "Presentation")]
    [InlineData("Dashboard", "Presentation.Contracts")]
    [InlineData("Dashboard", "Presentation.Mappings")]
    [InlineData("Dashboard", "tests/FoodDiary.Modules.Dashboard.Application.Tests")]
    [InlineData("Dashboard", "tests/FoodDiary.Modules.Dashboard.Infrastructure.Tests")]
    [InlineData("Dashboard", "tests/FoodDiary.Modules.Dashboard.Presentation.Tests")]
    [InlineData("Dietologist", "Application")]
    [InlineData("Dietologist", "Application.Abstractions")]
    [InlineData("Dietologist", "Contracts")]
    [InlineData("Dietologist", "Domain")]
    [InlineData("Dietologist", "Infrastructure")]
    [InlineData("Dietologist", "PersistenceModel")]
    [InlineData("Dietologist", "Presentation")]
    [InlineData("Dietologist", "Presentation.Contracts")]
    [InlineData("Dietologist", "tests/FoodDiary.Modules.Dietologist.Application.Tests")]
    [InlineData("Dietologist", "tests/FoodDiary.Modules.Dietologist.Domain.Tests")]
    [InlineData("Dietologist", "tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests")]
    [InlineData("Dietologist", "tests/FoodDiary.Modules.Dietologist.Presentation.Tests")]
    public void Projects_UseProjectNamesAndFolderNamespaces(string module, string project) {
        string directory = ArchitectureTestPaths.FromRoot("Modules", module, project);
        string projectFile = Assert.Single(Directory.EnumerateFiles(directory, "*.csproj"));
        string expectedRoot = project.StartsWith("tests/", StringComparison.Ordinal)
            ? project[6..] : $"FoodDiary.Modules.{module}." + project;
        Assert.Equal(expectedRoot, Path.GetFileNameWithoutExtension(projectFile));
        var document = XDocument.Load(projectFile);
        Assert.Empty(document.Descendants("RootNamespace"));
        Assert.Empty(document.Descendants("AssemblyName"));
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
    [InlineData("Weight")]
    [InlineData("Waist")]
    public void BodyMetrics_ExposesOnlyWriteAndProjectionRepositoryPorts(string measurement) {
        string ports = ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Application.Abstractions", measurement + "Entries", "Common");
        string[] actual = [.. Directory.GetFiles(ports, "I*Repository.cs")
            .Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal)];
        Assert.Equal([$"I{measurement}EntryReadModelRepository.cs", $"I{measurement}EntryWriteRepository.cs"], actual);
        foreach (string feature in new[] { $"Read{measurement}Entries", $"ReadLatest{measurement}Entry", $"Read{measurement}Summaries" }) {
            string handlerPath = ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Application", measurement + "Entries", "Queries", feature, feature + "QueryHandler.cs");
            string handler = File.ReadAllText(handlerPath);
            Assert.Contains($"I{measurement}EntryReadModelRepository", handler, StringComparison.Ordinal);
            Assert.DoesNotContain("FoodDiary.Modules.BodyMetrics.Domain", handler, StringComparison.Ordinal);
        }
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
