using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserAuthenticationQueryBudgetApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FreshSessionRequest_UsesTwoAuthenticationReadsAndDoesNotLoadCredentialOrProfileColumns(bool pollRecognitionOutsideRequest) {
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.AddHttpContextAccessor();
            services.AddSingleton<CommandCounter>();
            services.AddDbContext<SharedPersistenceDbContext>((provider, options) =>
                options.AddInterceptors(provider.GetRequiredService<CommandCounter>()));
        }));
        CommandCounter counter = isolated.Services.GetRequiredService<CommandCounter>();
        using HttpClient browser = isolated.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"query-budget-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        counter.Enabled = true;
        if (pollRecognitionOutsideRequest) {
            // Reproduce a notifier poll during measurement without depending on its timer.
            await using AsyncServiceScope pollingScope = isolated.Services.CreateAsyncScope();
            await pollingScope.ServiceProvider.GetRequiredService<IFoodRecognitionJobReader>()
                .GetUpdatesAsync(DateTime.UtcNow, CancellationToken.None);
        }
        using HttpResponseMessage listed = await browser.GetAsync("/api/v1/auth/sessions");
        counter.Enabled = false;
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        string[] commands = [.. counter.Commands];
        Assert.True(commands.Length == 4,
            $"A fresh session listing must need two authentication reads and its two guarded-listing queries. Observed SQL commands: {commands.Length}.{Environment.NewLine}{string.Join(Environment.NewLine, commands)}");
        foreach (string command in commands) {
            Assert.DoesNotContain("\"Password\"", command, StringComparison.Ordinal);
            Assert.DoesNotContain("\"UserPreferences\"", command, StringComparison.Ordinal);
            Assert.DoesNotContain("\"UserNutritionProfiles\"", command, StringComparison.Ordinal);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CommandCounter(IHttpContextAccessor httpContextAccessor) : DbCommandInterceptor {
        public bool Enabled { get; set; }
        public ConcurrentQueue<string> Commands { get; } = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (Enabled && httpContextAccessor.HttpContext?.Request.Path == "/api/v1/auth/sessions") {
                Commands.Enqueue(command.CommandText);
            }
            return ValueTask.FromResult(result);
        }
    }
}
