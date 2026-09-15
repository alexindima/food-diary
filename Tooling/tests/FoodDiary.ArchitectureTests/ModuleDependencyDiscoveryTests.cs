namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleDependencyDiscoveryTests {
    [Theory]
    [InlineData("using FoodDiary.Modules.Users.Contracts.Queries;", "Users")]
    [InlineData("using Requests = FoodDiary.Modules.Users.Contracts.Queries;", "Users")]
    [InlineData("using FoodDiary.Modules.Images.Service.Contracts.Models;", "Images")]
    [InlineData("using FoodDiary.Modules.Users.Application.Common;", "Users")]
    [InlineData("using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;", null)]
    [InlineData("using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;", null)]
    [InlineData("using FoodDiary.Modules.Users.Domain.Entities;", null)]
    [InlineData("class Example { const string Friend = \"FoodDiary.Modules.Users.Application.Tests\"; }", null)]
    public void Discovery_DistinguishesUseCaseDependenciesFromScalarTypesAndStrings(string source, string? expected) {
        string[] actual = [.. ModuleDependencyGraphTests.ReadReferencedApplicationModulesFromSource(source).Distinct(StringComparer.Ordinal)];
        string[] expectedModules = expected is null ? [] : [expected];
        Assert.Equal(expectedModules, actual);
    }
}
