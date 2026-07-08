using FoodDiary.Results;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Web.Api.Options;
using FoodDiary.Mediator;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class InitialAdminHostedService(
    IServiceProvider serviceProvider,
    IOptions<InitialAdminOptions> options,
    ILogger<InitialAdminHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        InitialAdminOptions settings = options.Value;

        AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Result<BootstrapInitialAdminModel> result = await sender.Send(
                new BootstrapInitialAdminCommand(settings.Email, settings.Password),
                cancellationToken).ConfigureAwait(false);

            if (result.IsFailure) {
                logger.LogWarning(
                    "Initial admin bootstrap failed for {Email}: {ErrorCode}",
                    settings.Email.Trim(),
                    result.Error.Code);
                return;
            }

            LogBootstrapResult(result.Value);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void LogBootstrapResult(BootstrapInitialAdminModel result) {
        switch (result.Status) {
            case BootstrapInitialAdminStatus.SkippedMissingPassword:
                logger.LogInformation("Initial admin bootstrap skipped because InitialAdmin:Password is not configured.");
                break;
            case BootstrapInitialAdminStatus.SkippedExistingUser:
                logger.LogInformation("Initial admin bootstrap skipped because user {Email} already exists.", result.Email);
                break;
            case BootstrapInitialAdminStatus.Created:
                logger.LogInformation("Initial admin user {Email} was created.", result.Email);
                break;
        }
    }
}
