using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SolutionModuleFolderTests {
    [Fact]
    public void SolutionFolders_HaveProjectsOrFilesInTheirSubtree() {
        AssertNoViolations(FindEmptyFolders(LoadSolution()), "Empty solution folders");
    }

    [Fact]
    public void SolutionFolders_HaveUniqueNames() {
        AssertNoViolations(FindDuplicateFolders(LoadSolution()), "Duplicate solution folders");
    }

    [Fact]
    public void SolutionProjects_HaveUniqueExistingPaths() {
        XDocument solution = LoadSolution();
        Assert.Multiple(
            () => AssertNoViolations(FindDuplicateProjects(solution), "Duplicate solution projects"),
            () => AssertNoViolations(
                FindMissingProjects(solution, path => File.Exists(ArchitectureTestPaths.FromRoot(path))),
                "Missing solution projects"));
    }

    [Theory]
    [InlineData("/Application/Core/")]
    [InlineData("/Tests/Core/")]
    public void CoreProjects_DoNotRegrowRedundantSolutionWrappers(string obsoletePrefix) {
        string[] obsoleteFolders = [.. LoadSolution().Descendants("Folder")
            .Select(FolderName)
            .Where(name => name.StartsWith(obsoletePrefix, StringComparison.OrdinalIgnoreCase))];

        AssertNoViolations(obsoleteFolders, $"Place projects directly under {obsoletePrefix.Replace("Core/", string.Empty, StringComparison.Ordinal)}");
    }

    [Fact]
    public void ModuleProjects_AreNestedUnderTheirOwningModuleSolutionFolder() {
        AssertNoViolations(FindMisplacedProjects(LoadSolution(), modules: true), "Misplaced module projects");
    }

    [Fact]
    public void ServiceSharedAndToolingProjects_AreNestedUnderTheirOwner() {
        AssertNoViolations(FindMisplacedProjects(LoadSolution(), modules: false), "Misplaced service, Shared or Tooling projects");
    }

    [Theory]
    [InlineData("Shared", "FoodDiary.Domain.Primitives.Tests")]
    [InlineData("Shared", "FoodDiary.Mediator.Tests")]
    [InlineData("Shared", "FoodDiary.Results.Tests")]
    [InlineData("Tooling", "FoodDiary.Analyzers.Tests")]
    [InlineData("Tooling", "FoodDiary.Development.Mcp.Tests")]
    public void SharedAndToolingTests_ArePhysicallyLocatedWithTheirOwner(string owner, string projectName) {
        string expectedPath = $"{owner}/tests/{projectName}/{projectName}.csproj";
        string[] paths = [.. LoadSolution().Descendants("Project").Select(ProjectPath)];

        Assert.Multiple(
            () => Assert.Contains(expectedPath, paths, StringComparer.Ordinal),
            () => Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(expectedPath))),
            () => Assert.False(File.Exists(ArchitectureTestPaths.FromRoot($"tests/{projectName}/{projectName}.csproj"))));
    }

    [Theory]
    [InlineData("Shared")]
    [InlineData("Tooling")]
    public void SharedAndToolingTests_ReuseCentralTestBuildSettings(string owner) {
        var settings = XDocument.Load(ArchitectureTestPaths.FromRoot($"{owner}/tests/Directory.Build.props"));
        string[] imports = [.. settings.Descendants("Import")
            .Select(import => ((string?)import.Attribute("Project") ?? string.Empty).Replace('\\', '/'))];

        Assert.Contains("../../tests/Directory.Build.props", imports, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("<Folder Name='/Empty/' />", "/Empty/")]
    [InlineData("<Folder Name='/A/' /><Folder Name='/AB/'><Project Path='P.csproj' /></Folder>", "/A/")]
    public void EmptyFolderDetection_ReportsEmptyBranchesWithoutMatchingSiblingPrefixes(string content, string expected) {
        Assert.Equal([expected], FindEmptyFolders(ParseSolution(content)));
    }

    [Fact]
    public void EmptyFolderDetection_ReportsParentsContainingOnlyEmptyChildren() {
        XDocument solution = ParseSolution("<Folder Name='/A/' /><Folder Name='/A/B/' />");

        Assert.Equal(["/A/", "/A/B/"], FindEmptyFolders(solution));
    }

    [Theory]
    [InlineData("<Folder Name='/A/'><Project Path='P.csproj' /></Folder>")]
    [InlineData("<Folder Name='/A/'><File Path='README.md' /></Folder>")]
    [InlineData("<Folder Name='/A/' /><Folder Name='/A/B/'><Project Path='P.csproj' /></Folder>")]
    [InlineData("<Folder Name='/A/' /><Folder Name='/A/B/'><File Path='README.md' /></Folder>")]
    public void EmptyFolderDetection_AcceptsContentAndMeaningfulSingleChildGroups(string content) {
        Assert.Empty(FindEmptyFolders(ParseSolution(content)));
    }

    [Theory]
    [InlineData("/Services/MailInbox/", "/services/mailinbox/")]
    [InlineData("/Services/MailInbox/", "\\Services\\MailInbox\\")]
    public void DuplicateFolderDetection_NormalizesCaseAndSeparators(string first, string second) {
        XDocument solution = ParseSolution($"<Folder Name='{first}' /><Folder Name='{second}' />");

        Assert.Single(FindDuplicateFolders(solution));
    }

    [Theory]
    [InlineData("Example/Project.csproj", "example/project.csproj")]
    [InlineData("Example/Project.csproj", "Example\\Project.csproj")]
    [InlineData("Example/Project.csproj", "./Example/../Example/Project.csproj")]
    public void DuplicateProjectDetection_NormalizesPathsAcrossSolutionFolders(string first, string second) {
        XDocument solution = ParseSolution($"<Project Path='{first}' /><Folder Name='/Tests/'><Project Path='{second}' /></Folder>");

        Assert.Single(FindDuplicateProjects(solution));
    }

    [Fact]
    public void DuplicateDetection_AllowsDistinctFoldersAndProjectsWithTheSameFilename() {
        XDocument solution = ParseSolution("""
            <Folder Name='/A/'><Project Path='A/Example.csproj' /></Folder>
            <Folder Name='/A/B/'><Project Path='B/Example.csproj' /></Folder>
            """);

        Assert.Multiple(
            () => Assert.Empty(FindDuplicateFolders(solution)),
            () => Assert.Empty(FindDuplicateProjects(solution)));
    }

    [Fact]
    public void MissingProjectDetection_ChecksRootAndNestedProjectsAndRequiresAPath() {
        XDocument solution = ParseSolution("""
            <Project Path='Missing.csproj' />
            <Folder Name='/Tests/'>
              <Project Path='Present.csproj' />
              <Project Path='OtherMissing.csproj' />
              <Project />
            </Folder>
            """);

        Assert.Equal(
            ["Missing.csproj", "OtherMissing.csproj", "<Project without Path>"],
            FindMissingProjects(solution, path => string.Equals(path, "Present.csproj", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("Modules/Example/Application/P.csproj", "/Modules/Example/", true, true)]
    [InlineData("Modules/Example/tests/P.csproj", "/Modules/Example/tests/", true, true)]
    [InlineData("Modules/Example/tests/P.csproj", "/Modules/Example/tests/Integration/", true, true)]
    [InlineData("Modules\\Example\\tests\\P.csproj", "/modules/example/Tests/", true, true)]
    [InlineData("./Modules/Example/tests/P.csproj", "/Modules/Example/tests/", true, true)]
    [InlineData("Modules/Example/tests/P.csproj", "/Modules/Example/", true, false)]
    [InlineData("Modules/Example/tests/P.csproj", "/Modules/Other/tests/", true, false)]
    [InlineData("Modules/Example/tests/P.csproj", "/Tests/Example/", true, false)]
    [InlineData("Modules/Example/tests/P.csproj", "/Modules/Example/testsExtra/", true, false)]
    [InlineData("Modules/Example/Domain/P.csproj", "/Modules/Other/", true, false)]
    [InlineData("Modules/Example/Domain/P.csproj", "/Modules/ExampleExtra/", true, false)]
    [InlineData("Services/MailInbox/Application/P.csproj", "/Services/MailInbox/", false, true)]
    [InlineData("Services/MailRelay/Application/P.csproj", "/Services/MailRelay/", false, true)]
    [InlineData("Services/MailInbox/tests/P.csproj", "/Services/MailInbox/Tests/", false, true)]
    [InlineData("Services/MailRelay/tests/P.csproj", "/Services/MailRelay/Tests/Integration/", false, true)]
    [InlineData("Services/MailInbox/tests/P.csproj", "/Tests/MailInbox/", false, false)]
    [InlineData("Services/MailRelay/tests/P.csproj", "/Services/MailRelay/", false, false)]
    [InlineData("Services/MailInbox/tests/P.csproj", "/Services/MailInbox/TestsExtra/", false, false)]
    [InlineData("Services/MailInbox/Application/P.csproj", "/Services/MailRelay/", false, false)]
    [InlineData("Shared/tests/P.csproj", "/Shared/tests/", false, true)]
    [InlineData("Tooling/tests/P.csproj", "/Tooling/tests/", false, true)]
    [InlineData("Tooling\\tests\\P.csproj", "/tooling/Tests/Integration/", false, true)]
    [InlineData("./Shared/tests/P.csproj", "/Shared/tests/", false, true)]
    [InlineData("Shared/tests/P.csproj", "/Tests/", false, false)]
    [InlineData("Tooling/tests/P.csproj", "/Tests/", false, false)]
    [InlineData("Shared/tests/P.csproj", "/Shared/", false, false)]
    [InlineData("Tooling/tests/P.csproj", "/Tooling/", false, false)]
    [InlineData("Shared/tests/P.csproj", "/Tooling/tests/", false, false)]
    [InlineData("Tooling/tests/P.csproj", "/Shared/tests/", false, false)]
    [InlineData("Shared/tests/P.csproj", "/Shared/testsExtra/", false, false)]
    [InlineData("Tooling/tests/P.csproj", "/Tooling/testsExtra/", false, false)]
    [InlineData("tests/Example.Tests/P.csproj", "/Tests/", true, true)]
    [InlineData("tests/Example.Tests/P.csproj", "/Tests/", false, true)]
    public void OwnershipDetection_UsesOwnerAndTestBoundaries(string project, string folder, bool modules, bool valid) {
        XDocument solution = ParseSolution($"<Folder Name='{folder}'><Project Path='{project}' /></Folder>");

        Assert.Equal(valid ? 0 : 1, FindMisplacedProjects(solution, modules).Length);
    }

    [Fact]
    public void OwnershipDetection_RejectsOwnedProjectsPlacedAtSolutionRoot() {
        XDocument solution = ParseSolution("<Project Path='Modules/Example/Application/P.csproj' /><Project Path='Services/MailInbox/tests/P.csproj' />");

        Assert.Multiple(
            () => Assert.Single(FindMisplacedProjects(solution, modules: true)),
            () => Assert.Single(FindMisplacedProjects(solution, modules: false)));
    }

    private static XDocument LoadSolution() => XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));

    private static XDocument ParseSolution(string content) => XDocument.Parse($"<Solution>{content}</Solution>");

    private static string FolderName(XElement folder) =>
        "/" + string.Join('/', ((string?)folder.Attribute("Name") ?? string.Empty)
            .Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)) + "/";

    private static string ProjectPath(XElement project) {
        string path = ((string?)project.Attribute("Path") ?? string.Empty).Replace('\\', '/');
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot,
                Path.GetFullPath(path, ArchitectureTestPaths.RepositoryRoot)).Replace('\\', '/');
    }

    private static string[] FindEmptyFolders(XDocument solution) {
        XElement[] folders = [.. solution.Descendants("Folder")];
        string[] populated = [.. folders
            .Where(folder => folder.Elements("Project").Any() || folder.Elements("File").Any())
            .Select(FolderName)];

        return [.. folders.Select(FolderName)
            .Where(name => !populated.Any(child => child.StartsWith(name, StringComparison.OrdinalIgnoreCase)))];
    }

    private static string[] FindDuplicateFolders(XDocument solution) =>
        FindDuplicates(solution.Descendants("Folder").Select(FolderName));

    private static string[] FindDuplicateProjects(XDocument solution) =>
        FindDuplicates(solution.Descendants("Project").Select(ProjectPath));

    private static string[] FindDuplicates(IEnumerable<string> paths) =>
        [.. paths.GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => $"{group.Key} ({group.Count()} entries)")];

    private static string[] FindMissingProjects(XDocument solution, Func<string, bool> exists) =>
        [.. solution.Descendants("Project").Select(ProjectPath)
            .Where(path => path.Length == 0 || !exists(path))
            .Select(path => path.Length == 0 ? "<Project without Path>" : path)];

    private static string[] FindMisplacedProjects(XDocument solution, bool modules) {
        var misplaced = new List<string>();
        foreach (XElement project in solution.Descendants("Project")) {
            string path = ProjectPath(project);
            string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2) {
                continue;
            }

            string expectedPrefix;
            if (modules && string.Equals(segments[0], "Modules", StringComparison.OrdinalIgnoreCase)) {
                expectedPrefix = $"/Modules/{segments[1]}/";
                if (segments.Length > 2 && string.Equals(segments[2], "tests", StringComparison.OrdinalIgnoreCase)) {
                    expectedPrefix += "tests/";
                }
            } else if (!modules && string.Equals(segments[0], "Services", StringComparison.OrdinalIgnoreCase) &&
                       (string.Equals(segments[1], "MailInbox", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(segments[1], "MailRelay", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(segments[1], "BugTriage", StringComparison.OrdinalIgnoreCase))) {
                expectedPrefix = $"/Services/{segments[1]}/";
                if (segments.Length > 2 && string.Equals(segments[2], "tests", StringComparison.OrdinalIgnoreCase)) {
                    expectedPrefix += "Tests/";
                }
            } else if (!modules &&
                       (string.Equals(segments[0], "Shared", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(segments[0], "Tooling", StringComparison.OrdinalIgnoreCase)) &&
                       string.Equals(segments[1], "tests", StringComparison.OrdinalIgnoreCase)) {
                expectedPrefix = $"/{segments[0]}/tests/";
            } else {
                continue;
            }

            XElement? folder = project.Ancestors("Folder").FirstOrDefault();
            string folderName = folder is null ? "/" : FolderName(folder);
            if (!folderName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)) {
                misplaced.Add($"{path} is nested under {folderName}; expected {expectedPrefix}");
            }
        }

        return [.. misplaced];
    }

    private static void AssertNoViolations(string[] violations, string message) {
        Assert.True(violations.Length == 0, $"{message}:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
