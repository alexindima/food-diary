using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerFeaturesRootNamespaceTests {
    [Fact]
    public void AddPresentationApi_RegistersRequiredGlobalFilters() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPresentationApi();
        using ServiceProvider provider = services.BuildServiceProvider();

        MvcOptions options = provider.GetRequiredService<IOptions<MvcOptions>>().Value;
        ServiceFilterAttribute telemetryFilter = Assert.Single(
            options.Filters.OfType<ServiceFilterAttribute>(),
            filter => filter.ServiceType == typeof(TelemetryActionFilter));
        ServiceFilterAttribute authenticationCookieFilter = Assert.Single(
            options.Filters.OfType<ServiceFilterAttribute>(),
            filter => filter.ServiceType == typeof(AuthenticationCookieResultFilter));

        Assert.Multiple(
            () => Assert.Equal(typeof(TelemetryActionFilter), telemetryFilter.ServiceType),
            () => Assert.Equal(typeof(AuthenticationCookieResultFilter), authenticationCookieFilter.ServiceType));
    }
}
