using FoodDiary.Persistence.Runtime.Persistence.Email;
using FoodDiary.Application.Abstractions.Email.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Persistence.Runtime;

public static partial class PersistenceRuntimeRegistration {
    private static void AddEmailPersistence(this IServiceCollection services) {
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
    }
}
