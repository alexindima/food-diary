using FoodDiary.Modules.Products.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationDependencyInjectionTests {
    [Fact]
    public void AddProductsApplication_RegistersProductServices() {
        var services = new ServiceCollection();

        services.AddProductsApplication();

        Assert.Equal(2, services.Count(descriptor => descriptor.ServiceType == typeof(IProductSearchSuggestionProvider)));
    }
}
