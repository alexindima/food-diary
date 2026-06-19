namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class RequiresDockerTheoryAttribute : TheoryAttribute {
    public RequiresDockerTheoryAttribute() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            Skip = reason;
        }
    }
}
