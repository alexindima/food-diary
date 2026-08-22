using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Infrastructure;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class RepositorySourceFingerprint {
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(30);

    public static async Task<string> ComputeAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        string pathsOutput = await RunGitAsync(
            repositoryRoot,
            ["-c", "core.fsmonitor=false", "ls-files", "-co", "--exclude-standard", "-z"],
            inputLines: null,
            cancellationToken).ConfigureAwait(false);
        string deletedOutput = await RunGitAsync(
            repositoryRoot,
            ["-c", "core.fsmonitor=false", "ls-files", "--deleted", "-z"],
            inputLines: null,
            cancellationToken).ConfigureAwait(false);
        HashSet<string> deletedPaths = [.. deletedOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))];
        string[] paths = [.. pathsOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .Where(ChangeSetSnapshotService.IsSourcePath)
            .Where(path => !deletedPaths.Contains(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
        string hashesOutput = await RunGitAsync(
            repositoryRoot,
            ["hash-object", "--stdin-paths"],
            paths,
            cancellationToken).ConfigureAwait(false);
        string[] hashes = hashesOutput.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hashes.Length != paths.Length) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"Git hashed {hashes.Length} source files for {paths.Length} repository paths.");
        }

        using var fingerprint = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (int index = 0; index < paths.Length; index++) {
            fingerprint.AppendData(Encoding.UTF8.GetBytes($"{paths[index]}:{hashes[index]}\n"));
        }
        return Convert.ToHexString(fingerprint.GetHashAndReset()).ToLowerInvariant();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task<string> RunGitAsync(
        string repositoryRoot,
        IReadOnlyList<string> arguments,
        IReadOnlyList<string>? inputLines,
        CancellationToken cancellationToken) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = "git",
                WorkingDirectory = repositoryRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        GitProcessEnvironment.ClearLocalRepositoryVariables(process.StartInfo);
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.Start();
        using CancellationTokenSource timeout = new(GitTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);
        try {
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            if (inputLines is not null) {
                foreach (string line in inputLines) {
                    await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync(linkedCancellation.Token).ConfigureAwait(false);
            string output = await outputTask.ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);
            if (process.ExitCode != 0) {
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.RepositoryNotFound,
                    $"Repository source fingerprint failed: {error.Trim()}");
            }
            return output;
        } catch (OperationCanceledException) {
            TryKill(process);
            if (timeout.IsCancellationRequested) {
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.Timeout,
                    $"Repository source fingerprint exceeded {GitTimeout}.");
            }
            throw;
        } catch {
            TryKill(process);
            throw;
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static void TryKill(Process process) {
        try {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
            }
        } catch (InvalidOperationException) {
        }
    }
}
