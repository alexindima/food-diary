using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Development.Mcp.Infrastructure;

internal static class DevelopmentMcpSourceFingerprint {
    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase) {
        ".cs", ".csproj", ".props", ".targets",
    };

    public static async Task<string> ComputeAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        string projectRoot = Path.Combine(repositoryRoot, "FoodDiary.Development.Mcp");
        IEnumerable<string> projectInputs = Directory
            .EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => SourceExtensions.Contains(Path.GetExtension(path)))
            .Where(path => !ContainsDirectory(path, "bin") && !ContainsDirectory(path, "obj"));
        IEnumerable<string> rootInputs = new[] {
            "Directory.Build.props", "Directory.Packages.props", "global.json",
        }
            .Select(path => Path.Combine(repositoryRoot, path))
            .Where(File.Exists);
        string[] inputs = [.. projectInputs
            .Concat(rootInputs)
            .OrderBy(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'), StringComparer.Ordinal)];

        using var fingerprint = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (int index = 0; index < inputs.Length; index++) {
            string path = inputs[index];
            cancellationToken.ThrowIfCancellationRequested();
            string relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
            byte[] content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            string line = $"{relativePath}:{Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant()}";
            fingerprint.AppendData(Encoding.UTF8.GetBytes(line));
            if (index < inputs.Length - 1) {
                fingerprint.AppendData("\n"u8);
            }
        }
        return Convert.ToHexString(fingerprint.GetHashAndReset()).ToLowerInvariant();
    }

    private static bool ContainsDirectory(string path, string directory) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains(directory, StringComparer.OrdinalIgnoreCase);
}
