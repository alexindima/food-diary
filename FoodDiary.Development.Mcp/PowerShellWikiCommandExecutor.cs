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
            throw new InvalidOperationException("The wiki command process could not be started.");
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
            throw new TimeoutException($"Wiki command '{command}' exceeded {CommandTimeout}.");
        } catch {
            TryKill(process);
            throw;
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"Wiki command '{command}' failed with exit code " +
                $"{process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}: {error.Trim()}");
        }

        return new WikiCommandResult(command, output.Trim(), repositoryRoot);
    }

    private static void TryKill(Process process) {
        if (!process.HasExited) {
            process.Kill(entireProcessTree: true);
        }
    }
}
