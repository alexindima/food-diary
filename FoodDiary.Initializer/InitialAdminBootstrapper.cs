using System.Diagnostics;
using System.Globalization;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Results;

namespace FoodDiary.Initializer;

internal static class InitialAdminBootstrapper {
    public static async Task BootstrapAsync(
        IInitialAdminBootstrapService bootstrapService,
        InitialAdminBootstrapOptions options,
        CancellationToken cancellationToken = default) {
        long startedTimestamp = Stopwatch.GetTimestamp();
        Console.WriteLine($"Bootstrapping initial admin {options.Email.Trim()}...");

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.Timeout);

        Result<BootstrapInitialAdminModel> result;
        try {
            result = await bootstrapService
                .BootstrapAsync(options.Email, options.Password, timeoutSource.Token)
                .ConfigureAwait(false);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            throw new TimeoutException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Initial admin bootstrap exceeded the {options.Timeout.TotalSeconds:0}-second timeout."));
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startedTimestamp);
        if (result.IsFailure) {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Initial admin bootstrap failed after {elapsed.TotalMilliseconds:0} ms: {result.Error.Code}"));
        }

        string outcome = result.Value.Status switch {
            BootstrapInitialAdminStatus.SkippedMissingPassword => "skipped because no password is configured",
            BootstrapInitialAdminStatus.SkippedExistingUser => "skipped because the user already exists",
            BootstrapInitialAdminStatus.Created => "created",
            _ => throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Unknown initial admin bootstrap status '{result.Value.Status}'.")),
        };
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Initial admin bootstrap {outcome} in {elapsed.TotalMilliseconds:0} ms."));
    }
}
