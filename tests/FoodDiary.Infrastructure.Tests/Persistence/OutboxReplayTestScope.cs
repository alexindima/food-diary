using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
internal sealed class OutboxReplayTestScope : IDisposable {
    private readonly ServiceProvider _provider;

    public OutboxReplayTestScope(FoodDiaryDbContext context, TimeProvider timeProvider) {
        var services = new ServiceCollection();
        services.AddSingleton(context);
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
    public void Dispose() => _provider.Dispose();
}
