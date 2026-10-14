using System;
using System.Collections.Generic;
using System.IO;

namespace LlmWiki.TestDiscovery {
    public static class SourceMatching {
        public static Dictionary<string, string> ReadSources(string root, string[] paths) {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in paths) {
                var absolutePath = Path.Combine(root, path);
                if (File.Exists(absolutePath)) {
                    result[path.Replace('\\', '/')] = File.ReadAllText(absolutePath);
                }
            }
            return result;
        }

        public static int[] FindPrefix(string[] paths, string prefix) {
            var matches = new List<int>();
            for (var index = 0; index < paths.Length; index++) {
                if (paths[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { matches.Add(index); }
            }
            return matches.ToArray();
        }

        public static Dictionary<string, int[]> FindContentMatches(string[] names, string[] contents, bool ignoreCase) {
            var result = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (var name in names) {
                if (result.ContainsKey(name)) { continue; }
                var matches = new List<int>();
                for (var index = 0; index < contents.Length; index++) {
                    if (contents[index].IndexOf(name, comparison) >= 0) { matches.Add(index); }
                }
                result.Add(name, matches.ToArray());
            }
            return result;
        }
    }
}
