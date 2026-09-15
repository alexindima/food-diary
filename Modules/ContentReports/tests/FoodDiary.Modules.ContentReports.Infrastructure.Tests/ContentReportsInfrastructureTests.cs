using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.ContentReports.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportsInfrastructureTests {
    [Fact]
    public void AddContentReportsModule_RegistersOwnedAdapters() {
        var services = new ServiceCollection();
        services.AddScoped<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(provider => provider.GetRequiredService<FoodDiaryDbContext>());
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        IServiceCollection returned = services.AddContentReportsModule();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ContentReportRepository repository = scope.ServiceProvider.GetRequiredService<ContentReportRepository>();

        Assert.Same(services, returned);
        Assert.Same(repository, scope.ServiceProvider.GetRequiredService<IContentReportWriteRepository>());
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IContentReportReadModelRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IContentReportTargetReadService));
    }

    [Fact]
    public void ContextContainsOnlyOwnedReportEntity() {
        using var context = new ContentReportsDbContext(new DbContextOptionsBuilder<ContentReportsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        Assert.Equal(typeof(ContentReport), Assert.Single(context.Model.GetEntityTypes()).ClrType);
    }
}
