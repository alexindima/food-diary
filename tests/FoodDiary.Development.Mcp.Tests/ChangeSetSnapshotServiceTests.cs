namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ChangeSetSnapshotServiceTests {
    [Theory]
    [InlineData("R  new/path.cs\0old/path.cs\0", "new/path.cs")]
    [InlineData("C  copied/path.cs\0source/path.cs\0", "copied/path.cs")]
    public void ParseChangedPaths_UsesDestinationForRenameAndCopy(
        string porcelain,
        string expectedPath) {
        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal([expectedPath], result);
    }

    [Fact]
    public void ParseChangedPaths_PreservesOrdinaryDeletesAndUnicodePaths() {
        const string porcelain = " D deleted.cs\0?? каталог/новый файл.cs\0";

        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal(["deleted.cs", "каталог/новый файл.cs"], result);
    }

    [Theory]
    [InlineData("node_modules/package/index.js")]
    [InlineData("project/bin/output.dll")]
    [InlineData("dist/app.js")]
    [InlineData(".angular/cache/item")]
    [InlineData("TestResults/result.xml")]
    public void IsIgnoredWatcherPath_ExcludesGeneratedAndDependencyTrees(string path) {
        Assert.True(ChangeSetSnapshotService.IsIgnoredWatcherPath(path));
    }

    [Theory]
    [InlineData("Shared/FoodDiary.Mediator/IMediator.cs", "Shared/FoodDiary.Mediator", true)]
    [InlineData("Shared/FoodDiary.Mediator", "Shared/FoodDiary.Mediator/IMediator.cs", true)]
    [InlineData("FoodDiary.Web.Client/src/app/app.ts", "Shared/FoodDiary.Mediator", false)]
    [InlineData(".llm-wiki/generated/code-graph.sqlite", "Shared/FoodDiary.Mediator", false)]
    [InlineData(".llm-wiki/tools/wiki-tool.ps1", ".llm-wiki", true)]
    public void IsPathRelevantToScope_SeparatesUnrelatedChanges(
        string path,
        string scope,
        bool expected) {
        Assert.Equal(
            expected,
            ChangeSetSnapshotService.IsPathRelevantToScope(path, [scope]));
    }

    [Fact]
    public async Task GetAsync_RevalidatesHeadWhenBranchReferenceChangesOutsideWatcherRoot() {
        string repositoryRoot = Path.Combine(
            Path.GetTempPath(),
            $"fooddiary-snapshot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repositoryRoot);
        try {
            RunGit(repositoryRoot, "init", "--quiet");
            RunGit(repositoryRoot, "config", "user.email", "snapshot@example.invalid");
            RunGit(repositoryRoot, "config", "user.name", "Snapshot Test");
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "source.txt"), "one");
            RunGit(repositoryRoot, "add", "source.txt");
            RunGit(repositoryRoot, "commit", "--quiet", "-m", "first");
            string firstHead = RunGit(repositoryRoot, "rev-parse", "HEAD").Trim();
            string branch = RunGit(repositoryRoot, "symbolic-ref", "--short", "HEAD").Trim();
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "source.txt"), "two");
            RunGit(repositoryRoot, "add", "source.txt");
            RunGit(repositoryRoot, "commit", "--quiet", "-m", "second");

            using (ChangeSetSnapshotService service = new(TimeProvider.System, repositoryRoot)) {
                ChangeSetSnapshot initial = await service.GetAsync(CancellationToken.None);
                RunGit(repositoryRoot, "update-ref", $"refs/heads/{branch}", firstHead);

                ChangeSetSnapshot refreshed = await service.GetAsync(CancellationToken.None);

                Assert.False(string.Equals(initial.GitHead, refreshed.GitHead, StringComparison.Ordinal));
                Assert.Equal(firstHead, refreshed.GitHead, ignoreCase: false);
            }
        } finally {
            foreach (string path in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.Delete(repositoryRoot, recursive: true);
        }
    }

    private static string RunGit(string repositoryRoot, params string[] arguments) {
        using System.Diagnostics.Process process = new() {
            StartInfo = new System.Diagnostics.ProcessStartInfo {
                FileName = "git",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
        return output;
    }
}
