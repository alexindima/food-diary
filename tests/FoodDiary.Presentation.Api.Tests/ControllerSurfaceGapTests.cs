using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Fasting;
using FoodDiary.Presentation.Api.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ControllerSurfaceGapTests {
    public static TheoryData<Type, string, int> ControllerContracts => new() {
        { typeof(AdminDashboardController), "api/v{version:apiVersion}/admin/dashboard", 1 },
        { typeof(FastingReadController), "api/v{version:apiVersion}/fasting", 3 },
        { typeof(UserOverviewController), "api/v{version:apiVersion}/users", 1 },
        { typeof(WaistGoalsController), "api/v{version:apiVersion}/users/waist-goals", 1 },
        { typeof(WeightGoalsController), "api/v{version:apiVersion}/users/weight-goals", 1 },
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
