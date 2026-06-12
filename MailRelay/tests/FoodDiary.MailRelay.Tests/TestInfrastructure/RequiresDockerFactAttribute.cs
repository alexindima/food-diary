namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out string? reason)) {
            Skip = reason;
        }
    }
}
