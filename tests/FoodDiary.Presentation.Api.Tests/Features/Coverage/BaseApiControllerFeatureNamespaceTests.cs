using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Images;

namespace FoodDiary.Presentation.Api.Tests.Features.Coverage;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerFeatureNamespaceTests {
    public static TheoryData<Type> ControllersWithGlobalTelemetry => [
        typeof(AdminSsoController),
        typeof(AiFoodController),
        typeof(AiUsageController),
        typeof(AuthPasswordController),
        typeof(AuthSessionController),
        typeof(AuthTelegramController),
        typeof(ImagesController),
    ];

    [Theory]
    [MemberData(nameof(ControllersWithGlobalTelemetry))]
    public void Controller_DoesNotInjectASecondTelemetryObserver(Type controllerType) {
        Type[] constructorDependencies = [.. Assert.Single(controllerType.GetConstructors())
            .GetParameters()
            .Select(static parameter => parameter.ParameterType)];

        Assert.Equal([typeof(ISender)], constructorDependencies);
    }
}
