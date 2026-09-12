namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RoleAndQualityBoundaryTests {
    [Theory]
    [InlineData("Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Users.Domain")]
    [InlineData("Modules/Identity/Application/FoodDiary.Modules.Identity.Application.csproj", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Users.Domain")]
    [InlineData("Modules/Dietologist/Application/FoodDiary.Modules.Dietologist.Application.csproj", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Modules.Users.Domain")]
    [InlineData("Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj", "FoodDiary.Modules.Products.FoodQuality", "FoodDiary.Modules.Products.Domain")]
    [InlineData("Modules/Favorites/Application/FoodDiary.Application.Favorites.csproj", "FoodDiary.Modules.Products.FoodQuality", "FoodDiary.Modules.Products.Domain")]
    [InlineData("Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj", "FoodDiary.Modules.Products.FoodQuality", "FoodDiary.Modules.Products.Domain")]
    [InlineData("Modules/Recipes/Application/FoodDiary.Modules.Recipes.Application.csproj", "FoodDiary.Modules.Products.FoodQuality", "FoodDiary.Modules.Products.Domain")]
    public void Consumers_UseNarrowOwnerWithoutForeignDomain(string path, string owner, string foreignDomain) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(path);

        Assert.Contains(owner, references, StringComparer.Ordinal);
        Assert.DoesNotContain(foreignDomain, references, StringComparer.Ordinal);
    }

    [Fact]
    public void FoodQuality_DependsOnlyOnScalarContractsAndPrimitives() {
        Assert.Equal(
            ["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Products.Domain.Contracts"],
            ProjectReferenceReader.ReadProjectReferences(
                "Modules/Products/FoodQuality/FoodDiary.Modules.Products.FoodQuality.csproj"));

        Type[] publicTypes = typeof(FoodDiary.Domain.ValueObjects.FoodQualityScore).Assembly.GetExportedTypes();
        Assert.Equal(
            [typeof(FoodDiary.Domain.ValueObjects.FoodQualityGrade), typeof(FoodDiary.Domain.ValueObjects.FoodQualityScore)],
            publicTypes.OrderBy(type => type.FullName, StringComparer.Ordinal));
    }
}
