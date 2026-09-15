using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
internal sealed class OutboxReplayTestScope : IDisposable {
    private readonly ServiceProvider _provider;
    private readonly FoodDiaryDbContext _context;

    public OutboxReplayTestScope(FoodDiaryDbContext context, TimeProvider timeProvider) {
        _context = context;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton(context);
        services.AddSingleton<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(context);
        services.AddSingleton(timeProvider);
        services.AddScoped<IOutboxDeadLetterReplayService, OutboxDeadLetterReplayService>();
        services.AddScoped<IOutboxReplayStream, EmailOutboxReplayStream>();
        services.AddImagesInfrastructure();
        services.AddNotificationsPersistence();
        services.AddGamificationModule();
        _provider = services.BuildServiceProvider();
    }

    public IOutboxDeadLetterReplayService Service => _provider.GetRequiredService<IOutboxDeadLetterReplayService>();
    public IReadOnlyList<IOutboxReplayStream> Streams => _provider.GetServices<IOutboxReplayStream>().ToArray();
    public IEnumerable<EntityEntry> Entries => new[] { _context }.Cast<DbContext>().Concat(_context.ModuleContexts).SelectMany(item => item.ChangeTracker.Entries());
    public void ClearTracking() {
        _context.ChangeTracker.Clear();
        foreach (DbContext module in _context.ModuleContexts) { module.ChangeTracker.Clear(); }
    }
    public void Dispose() => _provider.Dispose();
}
