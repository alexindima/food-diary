namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UserRelatedReadBoundaryTests {
    [Theory]
    [InlineData("Fasting")]
    [InlineData("RecipeCommunity")]
    public void Consumers_UseUsersContractsWithoutReadingUsersSet(string module) {
        string path = $"Modules/{module}/Infrastructure/FoodDiary.Modules.{module}.Infrastructure.csproj";
        string[] references = ProjectReferenceReader.ReadProjectReferences(path);
        Assert.Contains("FoodDiary.Modules.Users.Contracts", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Users.Domain.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Users.Domain", references, StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(ArchitectureTestPaths.FromRoot($"Modules/{module}/Infrastructure/Persistence"), "*.cs", SearchOption.AllDirectories)) {
            string source = File.ReadAllText(file);
            Assert.DoesNotContain("context.Users", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Set<User>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Domain.Entities.Users", source, StringComparison.Ordinal);
        }
    }
}
