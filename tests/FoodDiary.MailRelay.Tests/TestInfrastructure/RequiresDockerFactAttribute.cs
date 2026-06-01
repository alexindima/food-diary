namespace FoodDiary.MailRelay.Tests.TestInfrastructure;

public sealed class RequiresDockerFactAttribute : FactAttribute {
    public RequiresDockerFactAttribute() {
        if (!DockerAvailability.IsAvailable(out var reason)) {
            Skip = reason;
        }
    }
}
