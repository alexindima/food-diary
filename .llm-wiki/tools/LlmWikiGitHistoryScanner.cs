using System;
using System.Collections.Generic;
using System.IO;

namespace LlmWiki.TestDiscovery {
    public sealed class Precedent {
        public string hash { get; set; } = "";
        public string shortHash { get; set; } = "";
        public string date { get; set; } = "";
        public string subject { get; set; } = "";
        public int score { get; set; }
        public string[] matchedIntentTokens { get; set; } = Array.Empty<string>();
        public string[] matchedPaths { get; set; } = Array.Empty<string>();
    }

    public sealed class HistoryResult {
        public int CommitCount { get; set; }
        public List<Precedent> Ranked { get; } = new List<Precedent>();
    }

    public static class HistoryScanner {
        public static HistoryResult Rank(string raw, string[] tokens, string[] paths, string[] prefixes) {
            if (paths.Length != prefixes.Length) { throw new ArgumentException("Scope paths and prefixes must align."); }
            var result = new HistoryResult();
            Precedent current = null;
            var matched = new List<string>();
            using (var reader = new StringReader(raw)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.Length == 0) { continue; }
                    if (line.StartsWith("@@@", StringComparison.Ordinal)) {
                        Complete(result, current, matched);
                        var parts = line.Substring(3).Split(new[] { '\t' }, 3);
                        current = new Precedent { hash = parts[0], shortHash = parts[0].Substring(0, 10), date = parts[1], subject = parts[2] };
                        var subject = parts[2].ToLowerInvariant();
                        var intent = new List<string>();
                        foreach (var token in tokens) { if (subject.Contains(token)) { intent.Add(token); } }
                        current.matchedIntentTokens = intent.ToArray();
                        matched.Clear();
                        result.CommitCount++;
                    } else if (current != null && matched.Count < 12 && !string.IsNullOrWhiteSpace(line)) {
                        var candidate = line.Trim().Replace('\\', '/');
                        for (var index = 0; index < paths.Length; index++) {
                            // Preserve PowerShell -eq and the explicit ordinal prefix test.
                            if (string.Equals(candidate, paths[index], StringComparison.InvariantCultureIgnoreCase) || candidate.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) {
                                matched.Add(candidate);
                                break;
                            }
                        }
                    }
                }
            }
            Complete(result, current, matched);
            return result;
        }

        private static void Complete(HistoryResult result, Precedent current, List<string> matched) {
            if (current == null) { return; }
            current.matchedPaths = matched.ToArray();
            current.score = (current.matchedIntentTokens.Length * 4) + (Math.Min(matched.Count, 5) * 3);
            if (current.score > 0) { result.Ranked.Add(current); }
        }
    }
}
