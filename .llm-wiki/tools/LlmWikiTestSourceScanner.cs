using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LlmWiki.TestDiscovery {
    public sealed class ScanResult {
        public Dictionary<string, string> Contents { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> Direct { get; } = new List<string>();
        public List<string> Declared { get; } = new List<string>();
    }

    public static class Scanner {
        public static ScanResult Scan(string repositoryRoot, string[] roots, Regex[] patterns, Regex[] declaredPatterns) {
            return Scan(repositoryRoot, roots, patterns, declaredPatterns, null, false);
        }

        public static ScanResult Scan(string repositoryRoot, string[] roots, Regex[] patterns, Regex[] declaredPatterns, Dictionary<string, string> contents, bool csharpOnly) {
            var result = new ScanResult { Contents = contents ?? new Dictionary<string, string>(StringComparer.Ordinal) };
            var excluded = new HashSet<string>(new[] { "bin", "obj", ".artifacts", "node_modules" }, StringComparer.OrdinalIgnoreCase);
            var suffix = new Regex(@"\.(spec|test)\.ts$", RegexOptions.IgnoreCase);
            var inventory = new List<FileInfo>();
            var options = new EnumerationOptions {
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Win32
            };
            foreach (var root in roots) {
                var absolute = Path.Combine(repositoryRoot, root);
                if (!Directory.Exists(absolute)) { continue; }
                var directories = new Stack<DirectoryInfo>();
                directories.Push(new DirectoryInfo(absolute));
                while (directories.Count > 0) {
                    var directory = directories.Pop();
                    foreach (var file in directory.EnumerateFiles("*", options)) {
                        if (!string.Equals(file.Extension, ".cs", StringComparison.OrdinalIgnoreCase) && (csharpOnly || !suffix.IsMatch(file.Name))) { continue; }
                        inventory.Add(file);
                    }
                    foreach (var child in directory.EnumerateDirectories("*", options)) {
                        if (excluded.Contains(child.Name) || (child.Attributes & FileAttributes.ReparsePoint) != 0) { continue; }
                        directories.Push(child);
                    }
                }
            }
            var texts = new string[inventory.Count];
            var direct = new bool[inventory.Count];
            var declared = new bool[inventory.Count];
            var hasOuterConcurrency = int.TryParse(Environment.GetEnvironmentVariable("LLM_WIKI_SMOKE_MAX_CONCURRENCY"), out var outerLimit);
            var outerConcurrency = hasOuterConcurrency ? Math.Max(1, Math.Min(8, outerLimit)) : 1;
            var concurrency = Math.Min(4, Math.Max(1, Environment.ProcessorCount / outerConcurrency));
            if (hasOuterConcurrency) { concurrency = Math.Min(concurrency, outerConcurrency); }
            Parallel.For(0, inventory.Count, new ParallelOptions { MaxDegreeOfParallelism = concurrency }, index => {
                var file = inventory[index];
                string content;
                // Captured contents are read-only during the parallel pass.
                if (!result.Contents.TryGetValue(file.FullName, out content)) { content = File.ReadAllText(file.FullName); }
                texts[index] = content;
                direct[index] = Matches(patterns, content);
                declared[index] = direct[index] && Matches(declaredPatterns, content);
            });
            for (var index = 0; index < inventory.Count; index++) {
                var file = inventory[index];
                result.Contents[file.FullName] = texts[index];
                if (!direct[index]) { continue; }
                var relative = file.FullName.Substring(repositoryRoot.Length + 1).Replace('\\', '/');
                result.Direct.Add(relative);
                if (declared[index]) { result.Declared.Add(relative); }
            }
            return result;
        }

        private static bool Matches(Regex[] patterns, string content) {
            if (patterns == null) { return false; }
            foreach (var pattern in patterns) { if (pattern != null && pattern.IsMatch(content)) { return true; } }
            return false;
        }
    }
}
