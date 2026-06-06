namespace FoodDiary.Infrastructure.Tests.Integration;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            Skip = reason;
        }
    }
}
