using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Tasks;

public sealed class NodeTaskToolExecutor : ITaskToolExecutor {
    private const int MaximumOutputCharacters = 128 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter<TaskToolAction>(JsonNamingPolicy.KebabCaseLower) },
    };

    public async Task<JsonElement> ExecuteAsync(TaskToolRequest request, CancellationToken cancellationToken) {
        string repositoryRoot = RepositoryRootResolver.Resolve();
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = "node",
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        process.StartInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "scripts", "ai", "task-tools.mjs"));
        if (!process.Start()) {
            throw new DevelopmentMcpException(DevelopmentMcpErrorCodes.TaskUnavailable, "Task tool process could not start.");
        }
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(120));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try {
            Task<string> output = ReadBoundedAsync(process.StandardOutput, linked.Token);
            Task<string> error = ReadBoundedAsync(process.StandardError, linked.Token);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request, JsonOptions).AsMemory(), linked.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await Task.WhenAll(process.WaitForExitAsync(linked.Token), output, error).ConfigureAwait(false);
            if (process.ExitCode != 0) {
                throw new DevelopmentMcpException(DevelopmentMcpErrorCodes.TaskUnavailable, (await error.ConfigureAwait(false)).Trim());
            }
            using var document = JsonDocument.Parse(await output.ConfigureAwait(false));
            return document.RootElement.Clone();
        } catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            throw new DevelopmentMcpException(DevelopmentMcpErrorCodes.Timeout, "Task operation exceeded its bounded timeout. A launched verification job has its own status and timeout.");
        } finally {
            if (!process.HasExited) {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            }
        }
    }

    private static async Task<string> ReadBoundedAsync(TextReader reader, CancellationToken cancellationToken) {
        char[] buffer = new char[4096];
        StringBuilder output = new();
        while (true) {
            int read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) { return output.ToString(); }
            if (output.Length + read > MaximumOutputCharacters) {
                throw new DevelopmentMcpException(DevelopmentMcpErrorCodes.TaskUnavailable, "Task diagnostics exceeded the output bound.");
            }
            output.Append(buffer, 0, read);
        }
    }
}
