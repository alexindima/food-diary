using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Meals.Application.Commands.CreateMeal;
using FoodDiary.Modules.Meals.Application.Commands.RepeatMeal;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AtomicMealBoundaryTests {
    [Theory]
    [InlineData(typeof(CreateMealCommand))]
    [InlineData(typeof(RepeatMealCommand))]
    public void MealCreationAndEvaluation_RequireHandlerAndSaveAtomicity(Type request) {
        Assert.True(typeof(IAtomicCommand).IsAssignableFrom(request));
    }

    [Fact]
    public void AtomicExecutionPort_DoesNotExposeDatabaseCapabilities() {
        Assert.Single(typeof(IAtomicCommandExecutor).GetMethods());
        Assert.Empty(typeof(IAtomicCommandExecutor).GetProperties());
        Assert.DoesNotContain(typeof(IAtomicCommandExecutor).Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || assembly.Name.StartsWith("Npgsql", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadComposition_UsesOwnerNutritionPolicy() {
        string path = ArchitectureTestPaths.FromRoot("FoodDiary.ReadModel.Composition", "Recipes", "RecipeOverviewReadService.cs");
        string source = File.ReadAllText(path);
        Assert.Contains("RecipeNutritionPolicy.Calculate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Round", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CalculateAutoNutrition", source, StringComparison.Ordinal);
    }
}
