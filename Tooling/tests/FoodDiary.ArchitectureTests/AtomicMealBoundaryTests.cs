using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Meals.Application.Commands.CreateMeal;
using FoodDiary.Modules.Meals.Application.Commands.RepeatMeal;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Mediator;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AtomicMealBoundaryTests {
    [Fact]
    public void EveryHandlerAcquiringImmediateEvaluationWrites_RequiresAtomicExecution() {
        Type[] consumers = [.. typeof(CreateMealCommand).Assembly.GetTypes()
            .Where(type => type.GetConstructors().Any(constructor => constructor.GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(IMealAchievementEvaluationRequest))))];
        Assert.NotEmpty(consumers);
        foreach (Type consumer in consumers) {
            Type[] handlers = [.. consumer.GetInterfaces().Where(type => type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))];
            Assert.NotEmpty(handlers);
            Assert.All(handlers, handler => Assert.True(typeof(IAtomicCommand).IsAssignableFrom(handler.GetGenericArguments()[0]),
                $"{consumer.FullName} acquires immediate outbox writes; its request must own an atomic handler/save transaction."));
        }
    }

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
