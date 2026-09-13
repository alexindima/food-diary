namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class NarrowConsumerContractTests {
    [Theory]
    [InlineData("FoodDiary.Modules.Users.Contracts")]
    [InlineData("FoodDiary.Modules.Lessons.Contracts")]
    [InlineData("FoodDiary.Modules.Notifications.Contracts")]
    [InlineData("FoodDiary.Modules.Identity.Contracts")]
    [InlineData("FoodDiary.Modules.BodyMetrics.Contracts")]
    [InlineData("FoodDiary.Modules.Images.Service.Contracts")]
    [InlineData("FoodDiary.Modules.Products.Contracts")]
    [InlineData("FoodDiary.Modules.Recipes.Contracts")]
    [InlineData("FoodDiary.Modules.Cycles.Contracts")]
    public void ConsumerProject_TransitiveClosureContainsOnlyNarrowContracts(string project) {
        IReadOnlyDictionary<string, string[]> graph = ProjectReferenceReader.ReadProductionProjectReferences();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        pending.Enqueue(project);
        while (pending.TryDequeue(out string? current)) {
            if (!visited.Add(current)) {
                continue;
            }

            foreach (string dependency in graph[current]) {
                Assert.True(
                    dependency.EndsWith(".Contracts", StringComparison.Ordinal)
                    || dependency is "FoodDiary.Results" or "FoodDiary.Domain.Primitives" or "FoodDiary.Mediator",
                    $"{project} exposes non-contract dependency through {current} -> {dependency}");
                pending.Enqueue(dependency);
            }
        }
    }

    [Theory]
    [InlineData("Notifications")]
    [InlineData("Identity")]
    [InlineData("BodyMetrics")]
    [InlineData("Images")]
    [InlineData("Products")]
    [InlineData("Recipes")]
    [InlineData("Cycles")]
    public void ForeignBusinessModules_DoNotReferenceInternalPorts(string owner) {
        foreach ((string project, string[] references) in ProjectReferenceReader.ReadProductionProjectReferences()) {
            if (!project.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                && !project.StartsWith("FoodDiary.Application.", StringComparison.Ordinal)) {
                continue;
            }

            if (project.StartsWith($"FoodDiary.Modules.{owner}.", StringComparison.Ordinal)
                || string.Equals(project, $"FoodDiary.Application.{owner}", StringComparison.Ordinal)
                || project is "FoodDiary.Application.Runtime") {
                continue;
            }

            Assert.DoesNotContain($"FoodDiary.Modules.{owner}.Application.Abstractions", references, StringComparer.Ordinal);
        }
    }
}
