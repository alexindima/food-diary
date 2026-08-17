using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class PowerShellWikiCommandExecutor : IWikiCommandExecutor {
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions RequestJsonOptions =
        new(JsonSerializerDefaults.Web);

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

        string requestPath = await WriteRequestAsync(arguments, cancellationToken).ConfigureAwait(false);

        try {
            return await ExecuteProcessAsync(
                command,
                requestPath,
                repositoryRoot,
                wikiPath,
                cancellationToken).ConfigureAwait(false);
        } finally {
            File.Delete(requestPath);
        }
    }

    private static async Task<WikiCommandResult> ExecuteProcessAsync(
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

        if (!process.Start()) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiCommandFailed,
                "The wiki command process could not be started.");
        }
        process.StandardInput.Close();

        Task<string> standardOutput = Task.Run(process.StandardOutput.ReadToEnd, cancellationToken);
        Task<string> standardError = Task.Run(process.StandardError.ReadToEnd, cancellationToken);
        var processExit = Task.Run(process.WaitForExit, CancellationToken.None);
        using CancellationTokenSource timeout = new(CommandTimeout);
        using var linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try {
            await processExit.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (timeout.IsCancellationRequested) {
            TryKill(process);
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.Timeout,
                $"Wiki command '{command}' exceeded {CommandTimeout}.");
        } catch {
            TryKill(process);
            throw;
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiCommandFailed,
                $"Wiki command '{command}' failed with exit code " +
                $"{process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {error.Trim()}");
        }

        string gitHead = await ServerStatusService
            .ReadGitHeadAsync(repositoryRoot, cancellationToken)
            .ConfigureAwait(false);
        return WikiOutputParser.Parse(command, output.Trim(), repositoryRoot, gitHead);
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

    private static void TryKill(Process process) {
        if (!process.HasExited) {
            process.Kill(entireProcessTree: true);
        }
    }
}
