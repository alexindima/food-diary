using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
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

        IServiceCollection returned = services.AddContentReportsModule();

        Assert.Same(services, returned);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IContentReportReadModelRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IContentReportWriteRepository));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IContentReportTargetReadService));
    }

    [Fact]
    public async Task IsReportableAsync_WithUnsupportedTargetType_ReturnsFalse() {
        await using FoodDiaryDbContext context = CreateContext();
        var repository = new ContentReportRepository(context);

        bool result = await repository.IsReportableAsync(
            UserId.New(), (ReportTargetType)int.MaxValue, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new FoodDiaryDbContext(options);
    }
}
