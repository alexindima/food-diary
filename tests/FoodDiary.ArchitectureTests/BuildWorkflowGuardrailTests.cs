using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BuildWorkflowGuardrailTests {
    [Fact]
    public void WikiCi_RunsIndependentWorkersAndRequiresTheirCombinedResult() {
        string workflow = File.ReadAllText(ArchitectureTestPaths.FromRoot(".github", "workflows", "ci-tests.yml"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        string focused = ReadJob("llm-wiki-focused");
        string audit = ReadJob("llm-wiki-audit");
        string gate = ReadJob("llm-wiki");
        Assert.Multiple(
            () => Assert.DoesNotContain("needs:", focused, StringComparison.Ordinal),
            () => Assert.DoesNotContain("needs:", audit, StringComparison.Ordinal),
            () => Assert.Contains("fetch-depth: 0", focused, StringComparison.Ordinal),
            () => Assert.Contains("fetch-depth: 0", audit, StringComparison.Ordinal),
            () => Assert.Contains("-VerificationProfile Focused", focused, StringComparison.Ordinal),
            () => Assert.DoesNotContain("-VerificationProfile Full", focused, StringComparison.Ordinal),
            () => Assert.Contains("if: github.event_name != 'pull_request'", audit, StringComparison.Ordinal),
            () => Assert.Contains("Test-LlmWikiTools.ps1 -Profile Full", audit, StringComparison.Ordinal),
            () => Assert.DoesNotContain("verify-full", audit, StringComparison.Ordinal),
            () => Assert.Contains("name: LLM Wiki verification", gate, StringComparison.Ordinal),
            () => Assert.Contains("if: always()", gate, StringComparison.Ordinal),
            () => Assert.Contains("needs: [llm-wiki-focused, llm-wiki-audit]", gate, StringComparison.Ordinal),
            () => Assert.Contains("${{ needs.llm-wiki-focused.result }}", gate, StringComparison.Ordinal),
            () => Assert.Contains("${{ needs.llm-wiki-audit.result }}", gate, StringComparison.Ordinal),
            () => Assert.Contains("Test-WikiCiResult.ps1", gate, StringComparison.Ordinal),
            () => Assert.Contains("Assert-WikiCiResult.ps1", gate, StringComparison.Ordinal),
            () => Assert.DoesNotContain("continue-on-error", focused + audit + gate, StringComparison.Ordinal),
            () => Assert.Contains("Test-LlmWikiApiCompatibility.ps1", focused, StringComparison.Ordinal),
            () => Assert.Contains("Get-LlmWikiDependencyChanges.ps1", focused, StringComparison.Ordinal),
            () => Assert.Contains("wiki.ps1 report", focused, StringComparison.Ordinal),
            () => Assert.Contains("llm-wiki-focused-failure-logs", focused, StringComparison.Ordinal),
            () => Assert.Contains("llm-wiki-audit-failure-logs", audit, StringComparison.Ordinal));

        string ReadJob(string id) {
            Match match = Regex.Match(workflow, $"^  {Regex.Escape(id)}:\\n(?<body>.*?)(?=^  [a-zA-Z0-9_-]+:|\\z)",
                RegexOptions.Multiline | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
            Assert.True(match.Success, $"Missing CI job: {id}");
            return match.Groups["body"].Value;
        }
    }

    [Fact]
    public void CiAndPrePush_RunCompleteBackendSuitesWithBoundedParallelism() {
        string ciWorkflow = File.ReadAllText(ArchitectureTestPaths.FromRoot(".github", "workflows", "ci-tests.yml"));
        string prePush = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push"));
        string groupRunner = File.ReadAllText(ArchitectureTestPaths.FromRoot("scripts", "ci", "Invoke-BackendTests.ps1"));

        Assert.Multiple(
            () => Assert.Contains("Invoke-BackendTests.ps1 -Group fast", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("Invoke-BackendTests.ps1 -Group '${{ matrix.group }}'", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("Test-BackendTestGroups.ps1", ciWorkflow, StringComparison.Ordinal),
            () => Assert.DoesNotContain("tests/*/*.csproj", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("needs: backend-fast", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("max-parallel: 4", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("needs: [backend-format, backend-fast, backend-slow]", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains(".result -ne 'success'", ciWorkflow, StringComparison.Ordinal),
            () => Assert.Contains("foreach ($path in $selected)", groupRunner, StringComparison.Ordinal),
            () => Assert.DoesNotContain("--filter", groupRunner, StringComparison.Ordinal),
            () => Assert.Contains("dotnet test FoodDiary.slnx", prePush, StringComparison.Ordinal),
            () => Assert.Contains("--maxcpucount:1", prePush, StringComparison.Ordinal));
    }

    [Fact]
    public void CiTestGroups_AssignEveryRunnableProjectExactlyOnce() {
        using var plan = JsonDocument.Parse(File.ReadAllText(
            ArchitectureTestPaths.FromRoot("scripts", "ci", "backend-test-groups.json")));
        JsonElement[] groups = [.. plan.RootElement.GetProperty("groups").EnumerateArray()];
        string[] actualPaths = [.. groups.SelectMany(group => group.GetProperty("projects").EnumerateArray())
            .Select(project => project.GetString()!)];
        var solution = XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        var runnableNames = ProjectReferenceReader.ReadTestProjectNames()
            .Where(name => !string.Equals(name, "FoodDiary.Testing", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        string[] expectedPaths = [.. solution.Descendants("Project")
            .Select(project => project.Attribute("Path")!.Value)
            .Where(path => runnableNames.Contains(Path.GetFileNameWithoutExtension(path)))
            .Order(StringComparer.Ordinal)];

        Assert.Multiple(
            () => Assert.Equal(1, plan.RootElement.GetProperty("schemaVersion").GetInt32()),
            () => Assert.Equal(
                new[] { "fast", "integration-1", "integration-2", "integration-3", "mcp" },
                groups.Select(group => group.GetProperty("name").GetString()), StringComparer.Ordinal),
            () => Assert.All(groups, group => Assert.NotEmpty(group.GetProperty("projects").EnumerateArray())),
            () => Assert.Equal(expectedPaths, actualPaths.Order(StringComparer.Ordinal), StringComparer.Ordinal));
    }

    [Fact]
    public void Solution_IncludesEveryRunnableTestProject() {
        var solution = XDocument.Load(ArchitectureTestPaths.FromRoot("FoodDiary.slnx"));
        var solutionProjects = solution.Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] missingTestProjects = [.. ProjectReferenceReader.ReadTestProjectNames()
            .Where(project => !string.Equals(project, "FoodDiary.Testing", StringComparison.Ordinal))
            .Where(project => !solutionProjects.Contains(project))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missingTestProjects);
    }

    [Fact]
    public void GitHooks_KeepCommitsFastAndIsolatePushArtifacts() {
        string preCommit = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-commit"));
        string prePush = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push"));

        Assert.Multiple(
            () => Assert.Equal("# Keep local commits fast; full verification runs before push.\ngit diff --cached --check\n", preCommit.Replace("\r\n", "\n", StringComparison.Ordinal)),
            () => Assert.Contains("PRE_PUSH_ARTIFACTS_PATH=\".artifacts/pre-push/$$\"", prePush, StringComparison.Ordinal),
            () => Assert.True(CountOccurrences(prePush, "Clean-NestedDotnetArtifacts.ps1 -RootArtifactPath") >= 2),
            () => Assert.Equal(1, CountOccurrences(prePush, "dotnet build ")),
            () => Assert.Contains("dotnet format FoodDiary.slnx --verify-no-changes --no-restore", prePush, StringComparison.Ordinal),
            () => Assert.Contains("Invoke-LlmWikiIndexPipeline.ps1 -Check -AffectedOnly", prePush, StringComparison.Ordinal),
            () => Assert.Contains("npm run build:storybook", prePush, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Clean-NestedDotnetArtifacts.ps1 -IncludeRoot", prePush, StringComparison.Ordinal));
    }

    [Fact]
    public void PrePush_SkipsVerificationOnlyWhenEveryRefIsDeleted() {
        string prePush = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Web.Client", ".husky", "pre-push"));
        int deletionGuard = prePush.IndexOf("[ \"$saw_ref_update\" = true ] && [ \"$only_deletions\" = true ]", StringComparison.Ordinal);
        int powerShellRequirement = prePush.IndexOf("command -v pwsh", StringComparison.Ordinal);

        Assert.Multiple(
            () => Assert.Contains("while read -r local_ref local_object_name remote_ref remote_object_name", prePush, StringComparison.Ordinal),
            () => Assert.Contains("[ \"$local_ref\" != \"(delete)\" ] && [ \"$local_object_name\" != \"0000000000000000000000000000000000000000\" ]", prePush, StringComparison.Ordinal),
            () => Assert.Contains("only_deletions=false", prePush, StringComparison.Ordinal),
            () => Assert.True(deletionGuard >= 0, "The pure-deletion guard is missing."),
            () => Assert.True(powerShellRequirement > deletionGuard, "Pure ref deletions must exit before PowerShell is required."));
    }

    [Fact]
    public void DevelopmentMcpLauncher_LocksActiveSessionsAndCollectsStaleOnes() {
        string launcher = File.ReadAllText(ArchitectureTestPaths.FromRoot("scripts", "Start-FoodDiaryDevelopmentMcp.ps1"));

        Assert.Multiple(
            () => Assert.Contains("Remove-StaleSessionDirectories", launcher, StringComparison.Ordinal),
            () => Assert.Contains(".session.lock", launcher, StringComparison.Ordinal),
            () => Assert.Contains("[IO.FileShare]::None", launcher, StringComparison.Ordinal),
            () => Assert.Contains("^(?:[0-9a-fA-F]{32}|[0-9a-f]{64})$", launcher, StringComparison.Ordinal));
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;
}
