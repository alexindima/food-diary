using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ArchitectureDocumentationTests {
    [Theory]
    [InlineData("docs/ARCHITECTURE.md")]
    [InlineData("docs/architecture/ai-development-reliability.md")]
    public void ArchitectureGuides_LinkToExistingEvidence(string relativePath) {
        string path = ArchitectureTestPaths.FromRoot(relativePath);
        string source = File.ReadAllText(path);
        MatchCollection links = Regex.Matches(source, @"\]\((?<target>[^)]+)\)", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
        Assert.NotEmpty(links);
        foreach (Match link in links) {
            string target = link.Groups["target"].Value.Split('#')[0];
            if (target.Length == 0 || Uri.TryCreate(target, UriKind.Absolute, out _)) { continue; }
            string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target));
            Assert.True(File.Exists(resolved), $"{relativePath} references missing evidence: {target}");
        }
        Assert.DoesNotContain("Application/Abstractions", source, StringComparison.Ordinal);
    }
}
