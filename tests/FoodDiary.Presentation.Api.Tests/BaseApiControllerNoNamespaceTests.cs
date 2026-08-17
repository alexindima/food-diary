using System.Reflection;
using FoodDiary.Presentation.Api.Controllers;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerNoNamespaceTests {
    [Fact]
    public void BaseApiController_DoesNotExposeRequestTelemetryHelpers() {
        string[] telemetryMethods = [.. typeof(BaseApiController)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(static method => method.Name)
            .Where(static name => name.Contains("Observed", StringComparison.Ordinal) ||
                                  name.Contains("Observation", StringComparison.Ordinal))];

        Assert.Empty(telemetryMethods);
    }
}
