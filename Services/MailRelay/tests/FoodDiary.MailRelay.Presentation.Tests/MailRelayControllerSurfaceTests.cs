using FoodDiary.MailRelay.Presentation.Features.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FoodDiary.MailRelay.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayControllerSurfaceTests {
    public static TheoryData<Type, string, int> ControllerContracts => new() {
        { typeof(MailRelayDeliveryEventsController), "api/email/events", 2 },
        { typeof(MailRelayMessagesController), "api/email/messages", 1 },
    };

    [Theory]
    [MemberData(nameof(ControllerContracts))]
    public void ThinController_HasExpectedRouteAndDeclaredActionCount(
        Type controllerType,
        string expectedRoute,
        int expectedActionCount) {
        RouteAttribute route = Assert.Single(controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false).Cast<RouteAttribute>());
        int actionCount = controllerType.GetMethods()
            .Count(static method => method.DeclaringType is not null &&
                                    method.DeclaringType != typeof(object) &&
                                    method.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false).Length > 0);

        Assert.Multiple(
            () => Assert.Equal(expectedRoute, route.Template),
            () => Assert.Equal(expectedActionCount, actionCount));
    }
}
