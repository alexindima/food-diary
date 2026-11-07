using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LlmWiki.TestDiscovery {
    public static class RuntimeTopologyScanner {
        public static string Fingerprint(string repositoryRoot, Dictionary<string, string> hashes, string[] orderedPaths) {
            var prefixLength = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;
            var material = new StringBuilder();
            // The caller retains the legacy PowerShell culture-aware ordering.
            foreach (var path in orderedPaths) {
                material.Append(Path.GetFullPath(path).Substring(prefixLength).Replace('\\', '/')).Append('=').Append(hashes[path]).Append('\n');
            }
            using var hasher = SHA256.Create();
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(material.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        public static Dictionary<string, string> Capture(string repositoryRoot) {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var excluded = new HashSet<string>(new[] { "tests", "obj", "bin", ".artifacts", ".llm-wiki", "TestResults", "Migrations" }, StringComparer.OrdinalIgnoreCase);
            var options = new EnumerationOptions {
                AttributesToSkip = FileAttributes.Hidden,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Win32
            };
            using var hasher = SHA256.Create();
            var compose = Path.Combine(repositoryRoot, "docker-compose.yml");
            if (File.Exists(compose)) { result[compose] = Hash(compose, hasher); }
            var directories = new Stack<DirectoryInfo>();
            var files = new List<FileInfo>();
            directories.Push(new DirectoryInfo(repositoryRoot));
            while (directories.Count > 0) {
                var directory = directories.Pop();
                foreach (var file in directory.EnumerateFiles("*.cs", options)) {
                    if (file.Name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) || file.Name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) { continue; }
                    files.Add(file);
                }
                foreach (var child in directory.EnumerateDirectories("*", options)) {
                    if (!excluded.Contains(child.Name) && (child.Attributes & FileAttributes.ReparsePoint) == 0) { directories.Push(child); }
                }
            }
            var hashes = new string[files.Count];
            var hasOuterLimit = int.TryParse(Environment.GetEnvironmentVariable("LLM_WIKI_SMOKE_MAX_CONCURRENCY"), out var outerLimit);
            var concurrency = Math.Min(4, Math.Max(1, Environment.ProcessorCount / (hasOuterLimit ? Math.Max(1, outerLimit) : 1)));
            if (hasOuterLimit) { concurrency = Math.Min(concurrency, Math.Max(1, outerLimit)); }
            Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = concurrency },
                SHA256.Create,
                (index, state, localHasher) => { hashes[index] = Hash(files[index].FullName, localHasher); return localHasher; },
                localHasher => localHasher.Dispose());
            for (var index = 0; index < files.Count; index++) { result[files[index].FullName] = hashes[index]; }
            return result;
        }

        private static string Hash(string path, SHA256 hasher) {
            // Read the bytes on every request. Metadata or timestamps must not
            // conceal equal-size edits, and decoding/newline rules match .NET
            // File.ReadAllText used by the generator and legacy PowerShell path.
            var text = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n");
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
    }
}
