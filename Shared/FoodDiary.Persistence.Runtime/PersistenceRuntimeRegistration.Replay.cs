using FoodDiary.Outbox.Management.Contracts.Common.Abstractions.Outbox;
using FoodDiary.Persistence.Runtime.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Persistence.Runtime;

public static partial class PersistenceRuntimeRegistration {
    public static IServiceCollection AddOutboxReplayManagement(this IServiceCollection services) {
        services.AddScoped<IOutboxDeadLetterReplayService, OutboxDeadLetterReplayService>();
        return services;
    }
}
