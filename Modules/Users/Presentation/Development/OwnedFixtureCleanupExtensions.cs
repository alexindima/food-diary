using System.Globalization;
using System.Data.Common;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Commands.PurgeOwnedFixtureUsers;
using FoodDiary.Modules.Images.Service.Contracts.Commands.ProcessOwnedImageDeletions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodDiary.Modules.Users.Presentation.Development;

public static class OwnedFixtureCleanupExtensions {
    extension(WebApplication app) {
        /// <summary>One-shot developer composition, without opening an HTTP listener or running scheduled jobs.</summary>
        public async Task<bool> TryRunOwnedFixtureCleanupAsync(string[] args, CancellationToken cancellationToken = default) {
            int flag = Array.IndexOf(args, "--purge-owned-fixtures");
            if (flag < 0) {
                return false;
            }
            string? configuration = Environment.GetEnvironmentVariable("FOODDIARY_TASK_CONFIG");
            if (!app.Environment.IsDevelopment() || flag + 1 >= args.Length || string.IsNullOrWhiteSpace(configuration) || !Path.IsPathFullyQualified(configuration)) {
                throw new InvalidOperationException("Fixture cleanup requires an owned Development task configuration.");
            }
            string runtimeDirectory = Path.GetDirectoryName(Path.GetFullPath(configuration))!;
            using var runtime = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(runtimeDirectory, "runtime.json"), cancellationToken).ConfigureAwait(false));
            string checkout = runtime.RootElement.GetProperty("checkout").GetString()!;
            string name = Path.GetFileName(runtimeDirectory);
            string expectedDirectory = Path.GetFullPath(Path.Combine(checkout, ".artifacts", "task-runtime", name));
            string manifestPath = Path.GetFullPath(args[flag + 1]);
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false));
            Guid runId = manifest.RootElement.GetProperty("runId").GetGuid();
            string expectedManifest = Path.Combine(expectedDirectory, "fixtures", runId.ToString("D"), "manifest.json");
            var database = new DbConnectionStringBuilder { ConnectionString = app.Configuration.GetConnectionString("DefaultConnection") };
            if (!string.Equals(runtimeDirectory, expectedDirectory, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifestPath, expectedManifest, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(checkout), Path.GetFullPath(Directory.GetCurrentDirectory()), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(manifest.RootElement.GetProperty("checkout").GetString()!), Path.GetFullPath(checkout), StringComparison.OrdinalIgnoreCase) ||
                manifest.RootElement.GetProperty("runtimeId").GetGuid() != runtime.RootElement.GetProperty("id").GetGuid() ||
                !string.Equals(database["Host"].ToString(), "127.0.0.1", StringComparison.Ordinal) || !string.Equals(database["Database"].ToString(), "fooddiary_task", StringComparison.Ordinal) ||
                !string.Equals(database["Port"].ToString(), runtime.RootElement.GetProperty("ports").GetProperty("postgres").GetInt32().ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)) {
                throw new InvalidOperationException("Fixture manifest, checkout and database ownership must match.");
            }
            Guid[] ids = [.. manifest.RootElement.GetProperty("accounts").EnumerateObject().Select(account => account.Value.GetProperty("id").GetGuid())];
            string[] keys = [.. manifest.RootElement.GetProperty("objects").EnumerateArray().Select(item => item.GetProperty("key").GetString()!).Distinct(StringComparer.Ordinal)];
            if (keys.Length > 1000 || keys.Any(key => key.Contains('\\', StringComparison.Ordinal) || key.Split('/').Any(part => part is "." or "..") || !ids.Any(id => key.StartsWith($"users/{id:D}/images/", StringComparison.Ordinal)))) {
                throw new InvalidOperationException("Fixture object keys must remain within the owned user scope.");
            }
            AsyncServiceScope scope = app.Services.CreateAsyncScope();
            await using (scope.ConfigureAwait(false)) {
                int removed = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                    new PurgeOwnedFixtureUsersCommand(runId, ids, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime), cancellationToken).ConfigureAwait(false);
                int deletedObjects = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                    new ProcessOwnedImageDeletionsCommand(ids, keys), cancellationToken).ConfigureAwait(false);
                Console.WriteLine(JsonSerializer.Serialize(new { fixtureRunId = runId, removedUsers = removed, processedImageDeletions = deletedObjects }));
            }
            return true;
        }
    }
}
