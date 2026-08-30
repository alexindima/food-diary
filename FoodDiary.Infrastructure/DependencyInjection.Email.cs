using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence.Email;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddEmailPersistence(this IServiceCollection services) {
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
    }
}
