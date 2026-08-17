using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed record ServerRuntimeIdentity(
    int ProcessId,
    DateTimeOffset ProcessStartedAtUtc,
    string AssemblyVersion,
    string InformationalVersion,
    string ModuleVersionId,
    string AssemblySha256,
    DateTimeOffset? AssemblyLastWriteTimeUtc,
    string RepositoryHeadAtStartup,
    string? BuiltFromGitHead,
    string? BuildSourceFingerprint) {
    public static ServerRuntimeIdentity Capture(string repositoryHeadAtStartup) {
        Assembly assembly = typeof(ServerRuntimeIdentity).Assembly;
        string assemblyPath = assembly.Location;
        FileInfo assemblyFile = new(assemblyPath);
        using var process = Process.GetCurrentProcess();
        using FileStream assemblyStream = File.OpenRead(assemblyPath);
        string assemblySha256 = Convert.ToHexString(SHA256.HashData(assemblyStream)).ToLowerInvariant();

        return new ServerRuntimeIdentity(
            Environment.ProcessId,
            new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero),
            assembly.GetName().Version?.ToString() ?? "unknown",
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            assembly.ManifestModule.ModuleVersionId.ToString("D"),
            assemblySha256,
            assemblyFile.Exists ? new DateTimeOffset(assemblyFile.LastWriteTimeUtc) : null,
            repositoryHeadAtStartup,
            NullIfWhiteSpace(Environment.GetEnvironmentVariable("FOODDIARY_MCP_BUILD_GIT_HEAD")),
            NullIfWhiteSpace(Environment.GetEnvironmentVariable("FOODDIARY_MCP_BUILD_SOURCE_FINGERPRINT")));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
