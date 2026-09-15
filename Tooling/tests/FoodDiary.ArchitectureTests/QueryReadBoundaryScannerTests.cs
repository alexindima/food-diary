namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class QueryReadBoundaryScannerTests {
    [Theory]
    [InlineData("Item", true)]
    [InlineData("Task<Item>", true)]
    [InlineData("Task<IReadOnlyList<Item>>", true)]
    [InlineData("Item[]", true)]
    [InlineData("Task<(IReadOnlyList<Item> Items, int Total)>", true)]
    [InlineData("Task<int>", false)]
    [InlineData("Task<bool>", false)]
    [InlineData("Task<IReadOnlyList<ItemModel>>", false)]
    public void Queries_AreCheckedByReturnedTypesInsteadOfRepositoryNames(string returnType, bool violates) {
        string source = $$"""
            using FoodDiary.Modules.Example.Domain.Entities;
            class ItemModel;
            interface IExampleReadRepository { {{returnType}} Read(); }
            class Query(IExampleReadRepository repository) { object Handle() => repository.Read(); }
            namespace FoodDiary.Modules.Example.Domain.Entities { class Item; }
            """;
        Assert.Equal(violates, QueryReadBoundaryScanner.FindViolations([("Query.cs", source)]).Length > 0);
    }
}
