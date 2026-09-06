namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class DirectOwnerContractReferenceTests {
    [Theory]
    [InlineData("FoodDiary.Modules.Dashboard.Contracts")]
    [InlineData("FoodDiary.Modules.Products.Contracts")]
    [InlineData("FoodDiary.Modules.Recipes.Contracts")]
    [InlineData("FoodDiary.Modules.Users.Application.Abstractions")]
    [InlineData("FoodDiary.Modules.Identity.Application.Abstractions")]
    [InlineData("FoodDiary.Modules.Billing.Application.Abstractions")]
    [InlineData("FoodDiary.Modules.Marketing.Application.Abstractions")]
    [InlineData("FoodDiary.Modules.Products.Domain.Contracts")]
    [InlineData("FoodDiary.Modules.Notifications.Application.Abstractions")]
    public void CentralAbstractions_DoNotReExportUnusedOwnerContracts(string projectName) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj");

        Assert.DoesNotContain(projectName, references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("Modules/Statistics/Application/FoodDiary.Modules.Statistics.Application.csproj", "FoodDiary.Modules.Dashboard.Contracts")]
    [InlineData("Modules/Products/Infrastructure/FoodDiary.Modules.Products.Infrastructure.csproj", "FoodDiary.Modules.Favorites.Application.Abstractions")]
    [InlineData("Modules/Meals/Application/FoodDiary.Modules.Meals.Application.csproj", "FoodDiary.Modules.Recipes.Contracts")]
    [InlineData("Modules/Identity/Application/FoodDiary.Modules.Identity.Application.csproj", "FoodDiary.Modules.Notifications.Application.Abstractions")]
    public void CrossModuleConsumers_ReferenceTheirContractOwnerDirectly(string projectPath, string contractProject) {
        string[] references = ProjectReferenceReader.ReadProjectReferences(projectPath);

        Assert.Contains(contractProject, references, StringComparer.Ordinal);
    }
}
