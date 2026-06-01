namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out var reason)) {
            Skip = reason;
        }
    }
}
