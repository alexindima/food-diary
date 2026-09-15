namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class RepositoryFileDiscovery {
    // Prune before descent: filtering resulting files still traverses build worktrees
    // and package junctions, which can dwarf the actual repository.
    public static IEnumerable<string> EnumerateFiles(string root, string pattern) {
        if (!Directory.Exists(root)) {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory)) {
            foreach (string file in Directory.EnumerateFiles(directory, pattern)) {
                yield return file;
            }

            foreach (string child in Directory.EnumerateDirectories(directory)) {
                string name = Path.GetFileName(child);
                if (name.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(".artifacts", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(".angular", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                pending.Push(child);
            }
        }
    }
}
