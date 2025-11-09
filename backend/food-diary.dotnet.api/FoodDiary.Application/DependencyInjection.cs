using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Common.Behaviors;
using FluentValidation;
using System.Reflection;

namespace FoodDiary.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR - регистрирует все IRequestHandler из сборки
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Добавляем ValidationBehavior в pipeline
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation - регистрируем все валидаторы из сборки
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
