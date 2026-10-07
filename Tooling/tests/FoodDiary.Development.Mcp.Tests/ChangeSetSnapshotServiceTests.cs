namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
public sealed class ChangeSetSnapshotServiceTests {
    [Fact]
    public void DefaultConstructor_UsesResolvedRepositoryAndDisposes() {
        using var service = new ChangeSetSnapshotService();

        Assert.NotNull(service);
    }

    [Theory]
    [InlineData("R  new/path.cs\0old/path.cs\0", "new/path.cs", "old/path.cs")]
    [InlineData(" R new/path.cs\0old/path.cs\0", "new/path.cs", "old/path.cs")]
    [InlineData("C  copied/path.cs\0source/path.cs\0", "copied/path.cs", null)]
    public void ParseChangedPaths_UsesBothRenameEndpointsAndOnlyCopyDestination(
        string porcelain,
        string expectedPath,
        string? expectedSourcePath) {
        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal(expectedSourcePath is null ? [expectedPath] : [expectedPath, expectedSourcePath], result);
    }

    [Fact]
    public void ParseChangedPaths_PreservesOrdinaryDeletesAndUnicodePaths() {
        const string porcelain = " D deleted.cs\0?? каталог/новый файл.cs\0";

        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal(["deleted.cs", "каталог/новый файл.cs"], result);
    }

    [Fact]
    public void ParseChangedPaths_PreservesCaseSensitiveGitNamesAndCanonicalHashOrder() {
        const string porcelain = "?? Scoped/alpha.cs\0?? Scoped/_z.cs\0?? Scoped/Alpha.cs\0";

        string[] result = ChangeSetSnapshotService.ParseChangedPaths(porcelain);

        Assert.Equal(["Scoped/Alpha.cs", "Scoped/_z.cs", "Scoped/alpha.cs"], result);
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

    [Theory]
    [InlineData("*", "anything")]
    [InlineData(".git/HEAD", "anything")]
    [InlineData(".git/index", "anything")]
    [InlineData("Folder/File.cs", " ")]
    public void IsPathRelevantToScope_CoversGlobalAndEmptyScopes(string path, string scope) {
        bool expected = !string.IsNullOrWhiteSpace(scope) ||
            path.StartsWith(".git/", StringComparison.Ordinal) ||
            string.Equals(path, "*", StringComparison.Ordinal);

        Assert.Equal(expected, ChangeSetSnapshotService.IsPathRelevantToScope(path, [scope]));
    }

    [Fact]
    public void WatcherError_InvalidatesCachedGeneration() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), $"fooddiary-snapshot-watcher-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repositoryRoot);
        try {
            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);
            System.Reflection.MethodInfo method = typeof(ChangeSetSnapshotService).GetMethod(
                "OnWatcherError",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            method.Invoke(service, [service, new ErrorEventArgs(new IOException("watcher failed"))]);

            System.Reflection.FieldInfo generation = typeof(ChangeSetSnapshotService).GetField(
                "_generation",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            Assert.Equal(1L, generation.GetValue(service));
        } finally {
            Directory.Delete(repositoryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WhenGitStatusFails_ThrowsRepositoryError() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), $"fooddiary-snapshot-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(repositoryRoot, ".git"));
        await File.WriteAllTextAsync(Path.Combine(repositoryRoot, ".git", "HEAD"), "detached-head");
        try {
            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);

            DevelopmentMcpException exception = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
                service.GetAsync(CancellationToken.None));

            Assert.Equal(DevelopmentMcpErrorCodes.RepositoryNotFound, exception.ErrorCode);
        } finally {
            Directory.Delete(repositoryRoot, recursive: true);
        }
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

    [Fact]
    public async Task GetAsync_WithScope_ProjectsExistingAndDeletedChanges() {
        string repositoryRoot = Path.Combine(
            Path.GetTempPath(),
            $"fooddiary-snapshot-scope-{Guid.NewGuid():N}");
        string scopedDirectory = Path.Combine(repositoryRoot, "Scoped");
        string unrelatedDirectory = Path.Combine(repositoryRoot, "Unrelated");
        Directory.CreateDirectory(scopedDirectory);
        Directory.CreateDirectory(unrelatedDirectory);
        try {
            RunGit(repositoryRoot, "init", "--quiet");
            RunGit(repositoryRoot, "config", "user.email", "snapshot@example.invalid");
            RunGit(repositoryRoot, "config", "user.name", "Snapshot Test");
            await File.WriteAllTextAsync(Path.Combine(scopedDirectory, "existing.cs"), "one");
            await File.WriteAllTextAsync(Path.Combine(scopedDirectory, "deleted.cs"), "delete me");
            await File.WriteAllTextAsync(Path.Combine(unrelatedDirectory, "other.cs"), "other");
            RunGit(repositoryRoot, "add", ".");
            RunGit(repositoryRoot, "commit", "--quiet", "-m", "baseline");
            await File.WriteAllTextAsync(Path.Combine(scopedDirectory, "existing.cs"), "two");
            File.Delete(Path.Combine(scopedDirectory, "deleted.cs"));
            await File.WriteAllTextAsync(Path.Combine(unrelatedDirectory, "other.cs"), "changed");

            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);
            ChangeSetSnapshot snapshot = await service.GetAsync(["Scoped"], CancellationToken.None);
            ChangeSetSnapshot refreshed = await service.RefreshAsync(CancellationToken.None);

            Assert.Equal(["Scoped/deleted.cs", "Scoped/existing.cs"], snapshot.ChangedPaths);
            Assert.Equal(
                ["Scoped/deleted.cs", "Scoped/existing.cs", "Unrelated/other.cs"],
                refreshed.ChangedPaths);
        } finally {
            foreach (string path in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.Delete(repositoryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WhenTrackedFileMovesOutOfScope_ReportsSourceDeletion() {
        string repositoryRoot = Path.Combine(Path.GetTempPath(), $"fooddiary-snapshot-rename-{Guid.NewGuid():N}");
        string resolvedRoot = Path.GetFullPath(repositoryRoot);
        if (!string.Equals(Path.GetDirectoryName(resolvedRoot), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal) ||
            !Path.GetFileName(resolvedRoot).StartsWith("fooddiary-snapshot-rename-", StringComparison.Ordinal)) {
            throw new InvalidOperationException("Unsafe snapshot fixture path.");
        }
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "Scoped"));
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "Unrelated"));
        try {
            RunGit(repositoryRoot, "init", "--quiet");
            RunGit(repositoryRoot, "config", "user.email", "snapshot@example.invalid");
            RunGit(repositoryRoot, "config", "user.name", "Snapshot Test");
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "Scoped", "source.cs"), "tracked source");
            RunGit(repositoryRoot, "add", ".");
            RunGit(repositoryRoot, "commit", "--quiet", "-m", "baseline");
            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);
            ChangeSetSnapshot initial = await service.GetAsync(["Scoped"], CancellationToken.None);

            RunGit(repositoryRoot, "mv", "Scoped/source.cs", "Unrelated/source.cs");
            await service.RefreshAsync(CancellationToken.None);
            ChangeSetSnapshot moved = await service.GetAsync(["Scoped"], CancellationToken.None);

            Assert.Equal(["Scoped/source.cs"], moved.ChangedPaths);
            Assert.False(string.Equals(initial.Fingerprint, moved.Fingerprint, StringComparison.Ordinal));
        } finally {
            foreach (string path in Directory.EnumerateFiles(resolvedRoot, "*", SearchOption.AllDirectories)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.Delete(resolvedRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("Unrelated/source.cs")]
    [InlineData(".artifacts/source.cs")]
    public void WatcherRename_InvalidatesOldScopeEvenWhenDestinationIsIgnored(string destination) {
        string repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"fooddiary-snapshot-rename-{Guid.NewGuid():N}"));
        if (!string.Equals(Path.GetDirectoryName(repositoryRoot), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)) {
            throw new InvalidOperationException("Unsafe snapshot fixture path.");
        }
        Directory.CreateDirectory(repositoryRoot);
        try {
            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);
            System.Reflection.MethodInfo handler = typeof(ChangeSetSnapshotService).GetMethod(
                "OnChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            System.Reflection.MethodInfo pending = typeof(ChangeSetSnapshotService).GetMethod(
                "HasRelevantPendingChange", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            handler.Invoke(service, [service, new RenamedEventArgs(WatcherChangeTypes.Renamed, repositoryRoot, destination, "Scoped/source.cs")]);

            Assert.Equal(true, pending.Invoke(service, [new[] { "Scoped" }]));
        } finally {
            Directory.Delete(repositoryRoot);
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
        FoodDiary.Development.Mcp.Infrastructure.GitProcessEnvironment
            .ClearLocalRepositoryVariables(process.StartInfo);
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
        return output;
    }

    [Theory]
    [InlineData("Scoped/source.cs", "Unrelated/source.cs")]
    [InlineData("Область/файл.cs", "Другая/файл.cs")]
    [InlineData("Scoped/source.cs", "Scoped/Source.cs")]
    [InlineData("Scoped/İ.cs", "Scoped/j.cs")]
    public async Task GetAsync_WithTrackedRename_MatchesRealNodeGraphFingerprint(string sourcePath, string destinationPath) {
        string repositoryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"fooddiary-snapshot-parity-{Guid.NewGuid():N}"));
        if (!string.Equals(Path.GetDirectoryName(repositoryRoot), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)) {
            throw new InvalidOperationException("Unsafe snapshot fixture path.");
        }
        string sourceRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
        Directory.CreateDirectory(Path.Combine(repositoryRoot, ".llm-wiki", "tools"));
        Directory.CreateDirectory(Path.Combine(repositoryRoot, ".llm-wiki", "policies"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(repositoryRoot, sourcePath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(repositoryRoot, destinationPath))!);
        try {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(sourceRoot, ".llm-wiki", "tools"), "*.mjs")) {
                File.Copy(file, Path.Combine(repositoryRoot, ".llm-wiki", "tools", Path.GetFileName(file)));
            }
            File.Copy(Path.Combine(sourceRoot, ".llm-wiki", "policies", "context-search-ranking.json"),
                Path.Combine(repositoryRoot, ".llm-wiki", "policies", "context-search-ranking.json"));
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, ".gitignore"), ".artifacts/\n");
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, sourcePath), "tracked source");
            RunGit(repositoryRoot, "init", "--quiet");
            RunGit(repositoryRoot, "config", "user.email", "snapshot@example.invalid");
            RunGit(repositoryRoot, "config", "user.name", "Snapshot Test");
            RunGit(repositoryRoot, "add", ".");
            RunGit(repositoryRoot, "commit", "--quiet", "-m", "baseline");
            RunGit(repositoryRoot, "mv", "-f", sourcePath, destinationPath);
            using var service = new ChangeSetSnapshotService(TimeProvider.System, repositoryRoot);
            ChangeSetSnapshot snapshot = await service.GetAsync(CancellationToken.None);
            Assert.Equal(new[] { sourcePath, destinationPath }.Order(StringComparer.Ordinal), snapshot.ChangedPaths, StringComparer.Ordinal);
            using System.Diagnostics.Process process = new() {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "node",
                    WorkingDirectory = repositoryRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            process.StartInfo.ArgumentList.Add(Path.Combine(repositoryRoot, ".llm-wiki", "tools", "code-graph.mjs"));
            process.StartInfo.ArgumentList.Add("status");
            FoodDiary.Development.Mcp.Infrastructure.GitProcessEnvironment.ClearLocalRepositoryVariables(process.StartInfo);
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
            process.Start();
            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
            try {
                await process.WaitForExitAsync(timeout.Token);
            } catch (OperationCanceledException) {
                if (!process.HasExited) { process.Kill(entireProcessTree: true); }
                throw;
            }
            Assert.True(process.ExitCode == 0, await error);
            using var status = System.Text.Json.JsonDocument.Parse(await output);
            Assert.Equal(snapshot.Fingerprint, status.RootElement.GetProperty("currentChangeSetFingerprint").GetString());
            Assert.Equal(snapshot.ChangedPaths.Count, status.RootElement.GetProperty("currentWorkspace").GetProperty("changedPathCount").GetInt32());
        } finally {
            foreach (string path in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)) {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.Delete(repositoryRoot, recursive: true);
        }
    }
}
