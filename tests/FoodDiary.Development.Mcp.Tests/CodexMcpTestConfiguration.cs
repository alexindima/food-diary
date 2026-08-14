using System.Text.Json;
using ModelContextProtocol.Client;

namespace FoodDiary.Development.Mcp.Tests;

[ExcludeFromCodeCoverage]
internal sealed record CodexMcpTestConfiguration(
    string Command,
    string[] Arguments,
    string WorkingDirectory,
    string[] EnabledTools) {
    public static CodexMcpTestConfiguration Load(string repositoryRoot) {
        string configPath = Path.Combine(repositoryRoot, ".codex", "config.toml");
        string[] lines = File.ReadAllLines(configPath);
        const string section = "[mcp_servers.fooddiary_development]";
        int start = Array.FindIndex(lines, line => string.Equals(line.Trim(), section, StringComparison.Ordinal));
        if (start < 0) {
            throw new InvalidOperationException($"{section} was not found in {configPath}.");
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = start + 1; index < lines.Length; index++) {
            string line = lines[index].Trim();
            if (line.Length > 0 && line[0] == '[') {
                break;
            }
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) {
                continue;
            }

            int separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0) {
                values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
        }

        string command = JsonSerializer.Deserialize<string>(values["command"])
                         ?? throw new InvalidOperationException("MCP command is missing.");
        string[] arguments = JsonSerializer.Deserialize<string[]>(values["args"])
                             ?? throw new InvalidOperationException("MCP arguments are missing.");
        string configuredWorkingDirectory = JsonSerializer.Deserialize<string>(values["cwd"])
                                            ?? throw new InvalidOperationException("MCP cwd is missing.");
        string[] enabledTools = JsonSerializer.Deserialize<string[]>(values["enabled_tools"])
                                ?? throw new InvalidOperationException("MCP enabled_tools are missing.");

        return new CodexMcpTestConfiguration(
            command,
            arguments,
            Path.GetFullPath(Path.Combine(repositoryRoot, configuredWorkingDirectory)),
            enabledTools);
    }

    public StdioClientTransportOptions CreateTransportOptions(string name) => new() {
        Name = name,
        Command = "dotnet",
        Arguments = [Path.Combine(AppContext.BaseDirectory, "FoodDiary.Development.Mcp.dll")],
        WorkingDirectory = WorkingDirectory,
        ShutdownTimeout = TimeSpan.FromSeconds(10),
    };
}
