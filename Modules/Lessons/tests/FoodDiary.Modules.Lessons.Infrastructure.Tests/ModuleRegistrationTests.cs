using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Lessons.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddLessonsModule_RegistersRepositoryAndAllPersistencePorts() {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddLessonsModule();

        Assert.Same(services, returned);
        ServiceDescriptor repository = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(INutritionLessonRepository));
        Assert.Equal(typeof(NutritionLessonRepository), repository.ImplementationType);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(INutritionLessonReadRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(INutritionLessonReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(INutritionLessonWriteRepository));
    }
}
