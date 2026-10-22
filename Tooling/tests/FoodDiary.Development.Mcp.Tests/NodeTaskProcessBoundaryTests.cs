using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Tasks;

namespace FoodDiary.Development.Mcp.Tests;

[Collection("PowerShell Wiki process")]
[ExcludeFromCodeCoverage]
public sealed class NodeTaskProcessBoundaryTests {
    [Fact]
    public async Task MissingNodeExecutable_PropagatesTheNativeLaunchFailure() {
        using var repository = new NodeRepository("process.stdout.write('{}');");
        string? originalPath = Environment.GetEnvironmentVariable("PATH");
        try {
            Environment.SetEnvironmentVariable("PATH", repository.Root);

            await Assert.ThrowsAsync<Win32Exception>(() => new NodeTaskToolExecutor().ExecuteAsync(
                new TaskToolRequest(TaskToolAction.Next), CancellationToken.None));
        } finally {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Fact]
    public async Task CallerCancellation_TerminatesTheOwnedNodeChildAndItsGrandchild() {
        using var repository = new NodeRepository(script: null);
        string childPath = Path.Combine(repository.Root, "child.pid");
        string grandchildPath = Path.Combine(repository.Root, "grandchild.pid");
        string grandchildScript = PublishIdentity(grandchildPath) + "setInterval(() => {}, 1000);";
        repository.WriteScript("import { spawn } from 'node:child_process'; "
            + "spawn(process.execPath, ['--input-type=module', '-e', " + JsonSerializer.Serialize(grandchildScript) + "], { stdio: 'ignore' }); "
            + PublishIdentity(childPath) + "setInterval(() => {}, 1000);");
        using var cancellation = new CancellationTokenSource();
        Task<JsonElement> execution = new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), cancellation.Token);
        Process? child = null;
        Process? grandchild = null;
        try {
            child = await ReadOwnedProcessAsync(childPath);
            grandchild = await ReadOwnedProcessAsync(grandchildPath);
            child.EnableRaisingEvents = true;
            grandchild.EnableRaisingEvents = true;
            Assert.False(child.HasExited);
            Assert.False(grandchild.HasExited);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await grandchild.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(child.HasExited);
            Assert.True(grandchild.HasExited);
        } finally {
            await cancellation.CancelAsync();
            foreach (Process? owned in new[] { child, grandchild }) {
                if (owned is { HasExited: false }) {
                    owned.Kill(entireProcessTree: true);
                    await owned.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
                owned?.Dispose();
            }
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
        }

        static string PublishIdentity(string path) => "const { writeFileSync, renameSync } = await import('node:fs'); "
            + "writeFileSync(" + JsonSerializer.Serialize(path + ".tmp") + ", String(process.pid)); "
            + "renameSync(" + JsonSerializer.Serialize(path + ".tmp") + ", " + JsonSerializer.Serialize(path) + "); ";
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task ToolTimeout_ReportsItsOwnDeadlineAndTerminatesTheOwnedNodeProcess() {
        using var repository = new NodeRepository(script: null);
        string identityPath = Path.Combine(repository.Root, "child.pid");
        string temporaryPath = identityPath + ".tmp";
        repository.WriteScript("import { writeFileSync, renameSync } from 'node:fs'; "
            + "writeFileSync(" + JsonSerializer.Serialize(temporaryPath) + ", String(process.pid)); "
            + "renameSync(" + JsonSerializer.Serialize(temporaryPath) + ", " + JsonSerializer.Serialize(identityPath) + "); "
            + "setInterval(() => {}, 1000);");
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(150));
        Task<JsonElement> execution = new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), harness.Token);
        Process? child = null;
        try {
            child = await ReadOwnedProcessAsync(identityPath);
            child.EnableRaisingEvents = true;

            DevelopmentMcpException failure = await Assert.ThrowsAsync<DevelopmentMcpException>(() => execution);

            Assert.Equal(DevelopmentMcpErrorCodes.Timeout, failure.ErrorCode);
            Assert.Contains("bounded timeout", failure.Message, StringComparison.Ordinal);
            Assert.False(harness.IsCancellationRequested);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(child.HasExited);
        } finally {
            await harness.CancelAsync();
            if (child is { HasExited: false }) {
                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { } catch (DevelopmentMcpException) { }
            child?.Dispose();
        }
    }

    [Fact]
    public async Task OutputAtTheExactBound_ReturnsAJsonValueThatSurvivesDocumentDisposal() {
        using var repository = new NodeRepository("process.stdout.write(JSON.stringify('x'.repeat(131070)));");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        JsonElement result = await new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), timeout.Token);

        Assert.Equal(JsonValueKind.String, result.ValueKind);
        Assert.Equal(new string('x', 131070), result.GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedStandardOutputOrDiagnostics_ReportsTheOutputBound(bool standardError) {
        string script = standardError
            ? "process.stdout.write('{}'); process.stderr.write('x'.repeat(131073));"
            : "process.stdout.write(JSON.stringify('x'.repeat(131071)));";
        using var repository = new NodeRepository(script);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        DevelopmentMcpException failure = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), timeout.Token));

        Assert.Multiple(
            () => Assert.Equal(DevelopmentMcpErrorCodes.TaskUnavailable, failure.ErrorCode),
            () => Assert.Equal("Task diagnostics exceeded the output bound.", failure.Message));
    }

    [Fact]
    public async Task NonzeroExit_ReturnsTrimmedDiagnosticsAsTaskUnavailable() {
        using var repository = new NodeRepository("process.stderr.write('  owned-node-failure  \\n'); process.exitCode = 3;");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        DevelopmentMcpException failure = await Assert.ThrowsAsync<DevelopmentMcpException>(() =>
            new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), timeout.Token));

        Assert.Multiple(
            () => Assert.Equal(DevelopmentMcpErrorCodes.TaskUnavailable, failure.ErrorCode),
            () => Assert.Equal("owned-node-failure", failure.Message));
    }

    [Fact]
    public async Task CallerCancellation_StopsTheOwnedNodeProcessAndRemainsCancellation() {
        using var repository = new NodeRepository(script: null);
        string identityPath = Path.Combine(repository.Root, "child.pid");
        string temporaryPath = identityPath + ".tmp";
        repository.WriteScript("import { writeFileSync, renameSync } from 'node:fs'; "
            + "writeFileSync(" + JsonSerializer.Serialize(temporaryPath) + ", String(process.pid)); "
            + "renameSync(" + JsonSerializer.Serialize(temporaryPath) + ", " + JsonSerializer.Serialize(identityPath) + "); "
            + "setInterval(() => {}, 1000);");
        using var cancellation = new CancellationTokenSource();
        Task<JsonElement> execution = new NodeTaskToolExecutor().ExecuteAsync(new TaskToolRequest(TaskToolAction.Next), cancellation.Token);
        Process? child = null;
        try {
            child = await ReadOwnedProcessAsync(identityPath);
            child.EnableRaisingEvents = true;
            Assert.False(child.HasExited);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(5)));
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(child.HasExited);
        } finally {
            await cancellation.CancelAsync();
            if (child is { HasExited: false }) {
                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            try { await execution.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
            child?.Dispose();
        }
    }

    private static async Task<Process> ReadOwnedProcessAsync(string identityPath) {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(5)) {
            if (File.Exists(identityPath)) {
                string identity = await File.ReadAllTextAsync(identityPath);
                return Process.GetProcessById(int.Parse(identity, CultureInfo.InvariantCulture));
            }
            // Poll only the identity file published atomically by the owned child.
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
        throw new TimeoutException("The owned Node process did not publish its identity within five seconds.");
    }

    [ExcludeFromCodeCoverage]
    private sealed class NodeRepository : IDisposable {
        private readonly string? _originalRoot = Environment.GetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable);
        private readonly string _artifactsRoot = Path.GetFullPath(Path.Combine(RepositoryRootResolver.Resolve(), ".artifacts"));
        public string Root { get; }

        public NodeRepository(string? script) {
            Root = Path.Combine(_artifactsRoot, "node-task-boundary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(Root, ".llm-wiki"));
            Directory.CreateDirectory(Path.Combine(Root, "scripts", "ai"));
            File.WriteAllText(Path.Combine(Root, ".llm-wiki", "wiki.ps1"), string.Empty);
            if (script is not null) { WriteScript(script); }
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, Root);
        }

        public void WriteScript(string script) => File.WriteAllText(Path.Combine(Root, "scripts", "ai", "task-tools.mjs"),
            "for await (const chunk of process.stdin) {}\n" + script);

        public void Dispose() {
            Environment.SetEnvironmentVariable(RepositoryRootResolver.RepositoryRootEnvironmentVariable, _originalRoot);
            string target = Path.GetFullPath(Root);
            if (!target.StartsWith(_artifactsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("node-task-boundary-", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Refusing cleanup outside the owned Node repository fixture.");
            }
            Directory.Delete(target, recursive: true);
        }
    }
}
