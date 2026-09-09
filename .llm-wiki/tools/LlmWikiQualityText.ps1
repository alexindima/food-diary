if (-not ('LlmWiki.QualityText' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;

namespace LlmWiki {
    public static class QualityText {
        public static string[] OrderPaths(string[] paths) {
            var unique = new HashSet<string>(paths, StringComparer.Ordinal);
            var ordered = new string[unique.Count];
            unique.CopyTo(ordered);
            var keys = new string[ordered.Length];
            for (var index = 0; index < ordered.Length; index++) {
                keys[index] = BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(ordered[index])).Replace("-", "");
            }
            Array.Sort(keys, ordered, StringComparer.Ordinal);
            return ordered;
        }

        public static Dictionary<string, List<string>> FindReferences(string[] names, string[] paths, string[] contents) {
            if (paths.Length != contents.Length) { throw new ArgumentException("Source paths and contents must align."); }
            var references = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var name in names) { references[name] = new List<string>(); }
            for (var index = 0; index < paths.Length; index++) {
                foreach (var entry in references) {
                    if (contents[index].IndexOf(entry.Key, StringComparison.Ordinal) >= 0) {
                        entry.Value.Add(paths[index]);
                    }
                }
            }
            return references;
        }

        public static int CountNonBlankLines(string content) {
            var count = 0;
            var hasContent = false;
            foreach (var character in content) {
                if (character == '\n') {
                    if (hasContent) { count++; }
                    hasContent = false;
                } else if (!char.IsWhiteSpace(character)) {
                    hasContent = true;
                }
            }
            return count + (hasContent ? 1 : 0);
        }
    }
}
'@
}
