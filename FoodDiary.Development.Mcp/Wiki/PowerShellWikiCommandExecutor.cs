using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class PowerShellWikiCommandExecutor : IWikiCommandExecutor {
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(3);
    private const int DefaultMaxOutputCharacters = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions RequestJsonOptions =
        new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _commandGate;
    private readonly int _maxOutputCharacters;
    private readonly WikiRuntimeTelemetry _telemetry;

    public PowerShellWikiCommandExecutor()
        : this(new WikiRuntimeTelemetry()) {
    }

    public PowerShellWikiCommandExecutor(WikiRuntimeTelemetry telemetry)
        : this(maxConcurrentCommands: 3, DefaultMaxOutputCharacters, telemetry) {
    }

    internal PowerShellWikiCommandExecutor(
        int maxConcurrentCommands,
        int maxOutputCharacters,
        WikiRuntimeTelemetry? telemetry = null) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentCommands, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxOutputCharacters, 1);
        _commandGate = new SemaphoreSlim(maxConcurrentCommands, maxConcurrentCommands);
        _maxOutputCharacters = maxOutputCharacters;
        _telemetry = telemetry ?? new WikiRuntimeTelemetry();
    }

    public async Task<WikiCommandResult> ExecuteAsync(
        string command,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken
    ) {
        string repositoryRoot = RepositoryRootResolver.Resolve();
        string wikiPath = Path.Combine(repositoryRoot, ".llm-wiki", "wiki.ps1");
        if (!File.Exists(wikiPath)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiUnavailable,
                $"Wiki entrypoint was not found at {wikiPath}.");
        }

        var requestStopwatch = Stopwatch.StartNew();
        string requestPath = await WriteRequestAsync(arguments, cancellationToken).ConfigureAwait(false);
        requestStopwatch.Stop();
        _telemetry.RecordCommandStage(command, "request-serialization", requestStopwatch.Elapsed);

        try {
            _telemetry.CommandQueued();
            var queueStopwatch = Stopwatch.StartNew();
            try {
                await _commandGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            } catch (OperationCanceledException) {
                queueStopwatch.Stop();
                _telemetry.RecordCommandStage(command, "queue-wait", queueStopwatch.Elapsed);
                _telemetry.CommandQueueCancelled();
                throw;
            }
            queueStopwatch.Stop();
            _telemetry.RecordCommandStage(command, "queue-wait", queueStopwatch.Elapsed);

            _telemetry.CommandStarted();
            var stopwatch = Stopwatch.StartNew();
            try {
                WikiCommandResult result = await ExecuteProcessAsync(
                    command,
                    requestPath,
                    repositoryRoot,
                    wikiPath,
                    cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _telemetry.CommandCompleted(command, stopwatch.Elapsed);
                return result;
            } catch (DevelopmentMcpException exception) {
                _telemetry.CommandFailed(
                    cancelled: false,
                    timedOut: string.Equals(
                        exception.ErrorCode,
                        DevelopmentMcpErrorCodes.Timeout,
                        StringComparison.Ordinal));
                throw;
            } catch (OperationCanceledException) {
                _telemetry.CommandFailed(cancelled: true, timedOut: false);
                throw;
            } catch {
                _telemetry.CommandFailed(cancelled: false, timedOut: false);
                throw;
            } finally {
                _commandGate.Release();
            }
        } finally {
            TryDelete(requestPath);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private async Task<WikiCommandResult> ExecuteProcessAsync(
        string command,
        string requestPath,
        string repositoryRoot,
        string wikiPath,
        CancellationToken cancellationToken) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh",
                WorkingDirectory = repositoryRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };
        GitProcessEnvironment.ClearLocalRepositoryVariables(process.StartInfo);

        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add(
            "[Console]::InputEncoding=[Text.UTF8Encoding]::new($false);" +
            "[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false);" +
            "$OutputEncoding=[Text.UTF8Encoding]::new($false);" +
            "& $env:FOODDIARY_WIKI_PATH $env:FOODDIARY_WIKI_COMMAND " +
            "-RequestFile $env:FOODDIARY_WIKI_REQUEST");
        process.StartInfo.Environment["FOODDIARY_WIKI_PATH"] = wikiPath;
        process.StartInfo.Environment["FOODDIARY_WIKI_COMMAND"] = command;
        process.StartInfo.Environment["FOODDIARY_WIKI_REQUEST"] = requestPath;

        var processStopwatch = Stopwatch.StartNew();
        Task<string>? standardOutput = null;
        Task<string>? standardError = null;
        try {
            if (!process.Start()) {
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.WikiCommandFailed,
                    "The wiki command process could not be started.");
            }
            process.StandardInput.Close();

            standardOutput = ReadBoundedAsync(
                process.StandardOutput,
                "standard output",
                cancellationToken);
            standardError = ReadBoundedAsync(
                process.StandardError,
                "standard error",
                cancellationToken);
            Task processExit = process.WaitForExitAsync(CancellationToken.None);
            using CancellationTokenSource timeout = new(CommandTimeout);
            using var linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            try {
                await Task.WhenAll(processExit, standardOutput, standardError)
                    .WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (timeout.IsCancellationRequested) {
                TryKill(process);
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.Timeout,
                    $"Wiki command '{command}' exceeded {CommandTimeout}.");
            } catch {
                TryKill(process);
                throw;
            }
        } finally {
            processStopwatch.Stop();
            _telemetry.RecordCommandStage(command, "process-round-trip", processStopwatch.Elapsed);
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiCommandFailed,
                $"Wiki command '{command}' failed with exit code " +
                $"{process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {error.Trim()}");
        }

        var resultStopwatch = Stopwatch.StartNew();
        try {
            string gitHead = await ServerStatusService
                .ReadGitHeadAsync(repositoryRoot, cancellationToken)
                .ConfigureAwait(false);
            return WikiOutputParser.Parse(command, output.Trim(), repositoryRoot, gitHead);
        } finally {
            resultStopwatch.Stop();
            _telemetry.RecordCommandStage(command, "result-processing", resultStopwatch.Elapsed);
        }
    }

    private static async Task<string> WriteRequestAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) {
        Dictionary<string, List<object>> grouped = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < arguments.Count;) {
            string name = arguments[index].TrimStart('-');
            bool isSwitch = index + 1 >= arguments.Count || arguments[index + 1].StartsWith('-');
            object value = isSwitch ? true : arguments[index + 1];
            if (!grouped.TryGetValue(name, out List<object>? values)) {
                values = [];
                grouped[name] = values;
            }
            values.Add(value);
            index += isSwitch ? 1 : 2;
        }

        var requestArguments = grouped.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count == 1 ? (object)pair.Value[0] : pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
        WikiCommandRequest request = new(1, requestArguments);
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-development-mcp", "requests");
        Directory.CreateDirectory(directory);
        string requestPath = Path.Combine(directory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(
            requestPath,
            JsonSerializer.Serialize(request, RequestJsonOptions),
            cancellationToken).ConfigureAwait(false);
        return requestPath;
    }

    private async Task<string> ReadBoundedAsync(
        TextReader reader,
        string streamName,
        CancellationToken cancellationToken) {
        char[] buffer = new char[8192];
        StringBuilder output = new();
        while (true) {
            int read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) {
                return output.ToString();
            }
            if (output.Length + read > _maxOutputCharacters) {
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.WikiCommandFailed,
                    $"Wiki command {streamName} exceeded " +
                    $"{_maxOutputCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture)} characters.");
            }
            output.Append(buffer, 0, read);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static void TryDelete(string path) {
        try {
            File.Delete(path);
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static void TryKill(Process process) {
        try {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
            }
        } catch (InvalidOperationException) {
        } catch (System.ComponentModel.Win32Exception) {
        }
    }
}
