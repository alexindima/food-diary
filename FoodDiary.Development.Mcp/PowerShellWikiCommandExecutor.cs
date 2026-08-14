using System.Diagnostics;

namespace FoodDiary.Development.Mcp;

public sealed class PowerShellWikiCommandExecutor : IWikiCommandExecutor {
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

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

        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh",
                WorkingDirectory = repositoryRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(wikiPath);
        process.StartInfo.ArgumentList.Add(command);
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start()) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiCommandFailed,
                "The wiki command process could not be started.");
        }
        process.StandardInput.Close();

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        using CancellationTokenSource timeout = new(CommandTimeout);
        using var linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try {
            await process.WaitForExitAsync(linkedCancellation.Token).ConfigureAwait(false);
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

    private static void TryKill(Process process) {
        if (!process.HasExited) {
            process.Kill(entireProcessTree: true);
        }
    }
}
