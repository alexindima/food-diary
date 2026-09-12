using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class TelegramIdentityConflictTests {
    [Fact]
    public void Registration_IsScopedAndDoesNotDuplicateTheSaveInterceptor() {
        var services = new ServiceCollection();
        services.AddUsersPersistence().AddUsersPersistence();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        TelegramIdentityConflictInterceptor firstInterceptor = Assert.Single(first.ServiceProvider.GetServices<ISaveChangesInterceptor>()
            .OfType<TelegramIdentityConflictInterceptor>());
        TelegramIdentityConflictInterceptor secondInterceptor = Assert.Single(second.ServiceProvider.GetServices<ISaveChangesInterceptor>()
            .OfType<TelegramIdentityConflictInterceptor>());
        Assert.NotSame(firstInterceptor, secondInterceptor);
        Assert.Same(firstInterceptor, Assert.Single(first.ServiceProvider.GetServices<ISaveChangesInterceptor>()
            .OfType<TelegramIdentityConflictInterceptor>()));
    }

    [Theory]
    [InlineData("IX_Users_TelegramUserId")]
    [InlineData("IX_Users_TelegramOidcIssuer_TelegramOidcSubject")]
    public void KnownIdentityConflict_DoesNotExposeProviderDetails(string constraint) {
        var provider = new PostgresException("private account data", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation, constraintName: constraint);
        var update = new DbUpdateException("private SQL", provider);
        DbUpdateConcurrencyException conflict = Assert.Throws<DbUpdateConcurrencyException>(() => TelegramIdentityConflictInterceptor.Translate(update));
        Assert.Null(conflict.InnerException);
        Assert.DoesNotContain("private", conflict.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("IX_Users_Email", PostgresErrorCodes.UniqueViolation)]
    [InlineData("IX_Users_TelegramUserId", PostgresErrorCodes.ForeignKeyViolation)]
    public void OtherFailures_AreNotTranslated(string constraint, string state) {
        var provider = new PostgresException("failure", "ERROR", "ERROR", state, constraintName: constraint);
        TelegramIdentityConflictInterceptor.Translate(new DbUpdateException("failure", provider));
    }
}
