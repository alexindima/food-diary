using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class FavoritesContractOwnershipTests {
    [Theory]
    [InlineData("Meal", 8)]
    [InlineData("Product", 8)]
    [InlineData("Recipe", 8)]
    public void Contracts_AreOwnedOnce_AndSeparatedFromRepositories(string kind, int ownerFileCount) {
        string area = $"Favorite{kind}s";
        string ownerRoot = ArchitectureTestPaths.FromRoot($"Modules/Favorites/Application/Abstractions/{area}");
        string publicRoot = ArchitectureTestPaths.FromRoot($"Modules/Favorites/Contracts/{area}");
        string centralRoot = ArchitectureTestPaths.FromRoot($"FoodDiary.Application.Abstractions/{area}");
        if (Directory.Exists(centralRoot)) {
            Assert.Empty(SourceScanner.SourceFiles(centralRoot));
        }
        Assert.Equal(ownerFileCount, SourceScanner.SourceFiles(ownerRoot).Count());
        Assert.Equal(kind.Equals("Meal", StringComparison.Ordinal) ? 4 : 2, SourceScanner.SourceFiles(publicRoot).Count());
        foreach (string suffix in new[] { "Repository", "ReadRepository", "ReadModelRepository", "WriteRepository" }) {
            Assert.True(File.Exists(Path.Combine(ownerRoot, "Common", $"IFavorite{kind}{suffix}.cs")));
        }
        Assert.True(File.Exists(Path.Combine(ownerRoot, "Common", $"Favorite{kind}Errors.cs")));
        Assert.True(File.Exists(Path.Combine(ownerRoot, "Models", $"Favorite{kind}ReadModel.cs")));
        Assert.True(File.Exists(Path.Combine(publicRoot, "Common", $"IFavorite{kind}ReadService.cs")));
        Assert.True(File.Exists(Path.Combine(publicRoot, "Models", $"Favorite{kind}Model.cs")));
    }

    [Fact]
    public void PublicReadContracts_DoNotExposeAggregateOrRepositoryTypes() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Favorites/Contracts");
        string[] identifiers = [.. SourceScanner.SourceFiles(root)
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
                .DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(node => node.Identifier.ValueText)];
        Assert.DoesNotContain(identifiers, name => name.EndsWith("Repository", StringComparison.Ordinal));
        foreach (string aggregate in new[] { "FavoriteMeal", "FavoriteProduct", "FavoriteRecipe", "Meal", "Product", "Recipe", "User" }) {
            Assert.DoesNotContain(aggregate, identifiers, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void SourceMealPort_RemainsFavoritesOwned_WithExplicitMealsConsumer() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Favorites/Application/Abstractions/FavoriteMeals/Common/IFavoriteMealSourceReadService.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules/Favorites/Application/Abstractions/FavoriteMeals/Models/FavoriteMealSourceModel.cs")));
        string[] references = ProjectReferenceReader.ReadProjectReferences("Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj");
        Assert.Contains("FoodDiary.Modules.Favorites.Application.Abstractions", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Favorites.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Application.Favorites", references, StringComparer.Ordinal);
    }
}
