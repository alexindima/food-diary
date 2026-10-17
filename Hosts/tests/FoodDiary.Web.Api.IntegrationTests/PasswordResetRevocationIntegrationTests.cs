using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Presentation.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class PasswordResetRevocationIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("change")]
    [InlineData("initial")]
    [InlineData("administrator")]
    public async Task PasswordReplacement_RejectsOldResetProofAgainstPostgres(string operation) {
        const string oldPassword = "OriginalPassword123!";
        const string newPassword = "ReplacementPassword456!";
        string email = $"reset-revoke-{Guid.NewGuid():N}@example.com";
        using HttpClient client = factory.CreateClient();
        (Guid userId, string token) = await RegisterAsync(client, email, oldPassword);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await db.Users.SingleAsync(u => u.Id == new UserId(userId));
            user.SetEmailConfirmed(isConfirmed: true);
            if (string.Equals(operation, "initial", StringComparison.Ordinal)) {
                // Model an already authenticated passwordless account without invoking an external provider.
                db.Entry(user).Property(u => u.HasPassword).CurrentValue = false;
            }
            await db.SaveChangesAsync();
        }

        using HttpResponseMessage issue = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new RequestPasswordResetHttpRequest(email));
        Assert.Equal(HttpStatusCode.NoContent, issue.StatusCode);
        PasswordResetMessage reset = factory.EmailSender.GetRequiredPasswordResetMessage(email);

        string endpoint = "/api/v1/users/password";
        if (string.Equals(operation, "administrator", StringComparison.Ordinal)) {
            token = await CreateAdministratorTokenAsync(client, oldPassword);
            endpoint = $"/api/v1/admin/users/{userId}/password";
        } else if (string.Equals(operation, "initial", StringComparison.Ordinal)) {
            endpoint += "/set";
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage replacement = operation switch {
            "change" => await client.PatchAsJsonAsync(endpoint, new ChangePasswordHttpRequest(oldPassword, newPassword)),
            "administrator" => await client.PatchAsJsonAsync(endpoint, new AdminUserSetPasswordHttpRequest(newPassword)),
            _ => await client.PatchAsJsonAsync(endpoint, new SetPasswordHttpRequest(newPassword)),
        };
        Assert.Equal(HttpStatusCode.NoContent, replacement.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        using HttpResponseMessage stale = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm", new ConfirmPasswordResetHttpRequest(userId, reset.Token, "AttackerPassword789!"));
        using var error = JsonDocument.Parse(await stale.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode),
            () => Assert.Equal("Authentication.InvalidToken", error.RootElement.GetProperty("error").GetString()),
            () => Assert.False(error.RootElement.TryGetProperty("accessToken", out _)));

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await db.Users.SingleAsync(u => u.Id == new UserId(userId));
            IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            Assert.Multiple(
                () => Assert.True(hasher.Verify(newPassword, user.Password)),
                () => Assert.Null(user.PasswordResetTokenHash),
                () => Assert.Null(user.PasswordResetTokenExpiresAtUtc),
                () => Assert.Null(user.PasswordResetSentAtUtc));
        }
        using HttpResponseMessage legitimate = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginHttpRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, legitimate.StatusCode);
    }

    private async Task<string> CreateAdministratorTokenAsync(HttpClient client, string password) {
        string email = $"reset-admin-{Guid.NewGuid():N}@example.com";
        (Guid id, _) = await RegisterAsync(client, email, password);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User admin = await db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role)
                .SingleAsync(u => u.Id == new UserId(id));
            Role? role = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Admin);
            if (role is null) {
                role = Role.Create(RoleNames.Admin);
                db.Roles.Add(role);
            }
            admin.ReplaceRoles([role]);
            admin.SetEmailConfirmed(isConfirmed: true);
            await db.SaveChangesAsync();
        }
        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, password));
        login.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(Guid UserId, string Token)> RegisterAsync(HttpClient client, string email, string password) {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterHttpRequest(email, password, "en"));
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (payload.RootElement.GetProperty("user").GetProperty("id").GetGuid(),
            payload.RootElement.GetProperty("accessToken").GetString()!);
    }
}
