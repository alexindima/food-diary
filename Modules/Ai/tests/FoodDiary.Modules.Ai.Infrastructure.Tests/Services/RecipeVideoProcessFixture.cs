using System.Diagnostics;
using System.Text.Json;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoProcessFixture : IAsyncLifetime {
    private readonly string _artifactsRoot = FindArtifactsRoot();
    public string Root { get; }
    public string ExecutablePath { get; private set; } = string.Empty;

    public RecipeVideoProcessFixture() => Root = Path.Combine(_artifactsRoot, "recipe-process-fixture-" + Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync() {
        string projectDirectory = Path.Combine(Root, "process helper");
        Directory.CreateDirectory(projectDirectory);
        string feed = Path.Combine(Root, "empty feed");
        Directory.CreateDirectory(feed);
        string project = Path.Combine(projectDirectory, "RecipeMediaFixture.csproj");
        await File.WriteAllTextAsync(project, ProjectSource).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Program.cs"), ProgramSource).ConfigureAwait(false);
        string build = Path.Combine(Root, "build");
        var start = new ProcessStartInfo("dotnet") {
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        string[] arguments = ["build", project, "--configuration", "Release", "--artifacts-path", build, "--disable-build-servers",
            "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false", "-p:ManagePackageVersionsCentrally=false",
            "-p:RestorePackagesWithLockFile=false", "-p:UseSharedCompilation=false", "-p:RestoreSources=" + feed,
            "-nodeReuse:false", "-maxcpucount:1", "--verbosity", "quiet"];
        foreach (string argument in arguments) { start.ArgumentList.Add(argument); }
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "1";
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("The owned media fixture build did not start.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try {
            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task<string> errors = process.StandardError.ReadToEndAsync(timeout.Token);
            await Task.WhenAll(process.WaitForExitAsync(timeout.Token), output, errors).ConfigureAwait(false);
            Assert.True(process.ExitCode == 0, await output.ConfigureAwait(false) + await errors.ConfigureAwait(false));
        } finally {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        string name = OperatingSystem.IsWindows() ? "RecipeMediaFixture.exe" : "RecipeMediaFixture";
        ExecutablePath = Assert.Single(Directory.GetFiles(build, name, SearchOption.AllDirectories));
    }

    public string NewStatePath() => Path.Combine(Root, "state-" + Guid.NewGuid().ToString("N") + ".json");

    public static MemoryStream Input(string mode, string statePath, bool webm = false) {
        byte[] header = webm ? [0x1a, 0x45, 0xdf, 0xa3, 0, 0, 0, 0, 0, 0, 0, 0] : [0, 0, 0, 12, 0x66, 0x74, 0x79, 0x70, 0, 0, 0, 0];
        byte[] request = JsonSerializer.SerializeToUtf8Bytes(new { mode, statePath });
        return new MemoryStream([.. header, .. request]);
    }

    public static async Task<RecipeVideoProcessState> ReadStateAsync(string path, CancellationToken cancellationToken) {
        long started = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(10)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) {
                return JsonSerializer.Deserialize<RecipeVideoProcessState>(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false), JsonSerializerOptions.Web)
                    ?? throw new InvalidDataException("The owned media process published an empty state.");
            }
            // The owned child publishes this external-resource signal by an atomic rename.
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException("The owned media process did not publish its state within ten seconds.");
    }

    public static string MediaDirectory(RecipeVideoProcessState state) {
        string directory = Path.GetFullPath(Path.GetDirectoryName(state.InputPath)!);
        string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(Path.GetDirectoryName(directory), temporaryRoot, comparison)
            || !Path.GetFileName(directory).StartsWith("fooddiary-recipe-", StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(state.InputPath), "video.bin", StringComparison.Ordinal)
            || !string.Equals(state.OutputPath, Path.Combine(directory, "audio.wav"), comparison)) {
            throw new InvalidOperationException("The process state does not identify its owned staged media directory.");
        }
        return directory;
    }

    public Task DisposeAsync() {
        string target = Path.GetFullPath(Root);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(_artifactsRoot + Path.DirectorySeparatorChar, comparison)
            || !Path.GetFileName(target).StartsWith("recipe-process-fixture-", StringComparison.Ordinal)) {
            throw new InvalidOperationException("Refusing cleanup outside the owned media process fixture.");
        }
        if (Directory.Exists(target)) { Directory.Delete(target, recursive: true); }
        return Task.CompletedTask;
    }

    private static string FindArtifactsRoot() {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent) {
            if (File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) {
                return Path.Combine(directory.FullName, ".artifacts");
            }
        }
        throw new DirectoryNotFoundException("The repository root for the owned media fixture was not found.");
    }

    private const string ProjectSource = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <EnableNETAnalyzers>false</EnableNETAnalyzers>
            <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
          </PropertyGroup>
          <ItemGroup><Compile Include="Program.cs" /></ItemGroup>
        </Project>
        """;

    private const string ProgramSource = """
        using System.Diagnostics;
        using System.Diagnostics.CodeAnalysis;
        using System.Text;
        using System.Text.Json;

        [assembly: ExcludeFromCodeCoverage]

        [ExcludeFromCodeCoverage]
        internal static class Program {
            private static async Task<int> Main(string[] args) {
                if (args.Length == 1 && args[0] == "wait-child") {
                    await Task.Delay(Timeout.InfiniteTimeSpan);
                    return 0;
                }
                string input = args[Array.IndexOf(args, "-i") + 1];
                string output = args[^1];
                byte[] video = await File.ReadAllBytesAsync(input);
                using JsonDocument request = JsonDocument.Parse(video.AsMemory(12));
                string mode = request.RootElement.GetProperty("mode").GetString()!;
                string statePath = request.RootElement.GetProperty("statePath").GetString()!;
                using Process? child = mode == "wait" ? Process.Start(new ProcessStartInfo(Environment.ProcessPath!) {
                    UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "wait-child" },
                }) : null;
                int? directoryMode = OperatingSystem.IsWindows() ? null : (int)File.GetUnixFileMode(Path.GetDirectoryName(input)!);
                string state = JsonSerializer.Serialize(new { pid = Environment.ProcessId, childPid = child?.Id,
                    inputPath = input, outputPath = output, arguments = args, directoryMode });
                await File.WriteAllTextAsync(statePath + ".tmp", state);
                File.Move(statePath + ".tmp", statePath);
                if (mode == "wait") {
                    await Task.Delay(Timeout.InfiniteTimeSpan);
                }
                if (mode == "error") {
                    await Console.Error.WriteAsync(new string('x', 2 * 1024 * 1024));
                    return 17;
                }
                if (mode == "missing") { return 0; }
                if (mode == "oversized") {
                    using FileStream oversized = File.Create(output);
                    oversized.SetLength(10 * 1024 * 1024 + 1);
                    return 0;
                }
                int dataLength = mode == "zero-duration" ? 0 : 32000;
                using FileStream file = File.Create(output);
                using BinaryWriter writer = new(file, Encoding.ASCII);
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataLength);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(16000); writer.Write(32000);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataLength);
                writer.Write(new byte[dataLength]);
                return 0;
            }
        }
        """;
}
