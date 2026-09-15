namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SharedAuthenticationContractBoundaryTests {
    [Fact]
    public void GenericApplicationContractsDoNotOwnAuthenticationFactories() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Shared/FoodDiary.Application.Contracts/Common/Abstractions/Results/Errors.Authentication.cs")));
        string[] references = ProjectReferenceReader.ReadProjectReferences("Shared/FoodDiary.Application.Contracts/FoodDiary.Application.Contracts.csproj");
        Assert.DoesNotContain(references, name => name.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal));
    }

    [Fact]
    public void SharedAuthenticationContractsHaveNoBusinessOwnerDependencies() {
        Assert.Equal(["FoodDiary.Results"], ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Authentication.Contracts/FoodDiary.Authentication.Contracts.csproj"));
        string limits = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Shared/FoodDiary.Authentication.Contracts/Authentication/Common/AuthenticationInputLimits.cs"));
        Assert.DoesNotContain("Telegram", limits, StringComparison.Ordinal);
        Assert.DoesNotContain("Google", limits, StringComparison.Ordinal);
        Assert.DoesNotContain("Sso", limits, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationRuntimeIsSharedAndRetainsOnlyExecutionDependencies() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj")));
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Mediator"], ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj"));
    }
}
