using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class AiDbContextTests {
    [Theory]
    [InlineData("23505", "public", "AiPromptTemplates", "IX_AiPromptTemplates_Key_Locale", true)]
    [InlineData("23505", "public", "AiPromptTemplates", "PK_AiPromptTemplates", false)]
    [InlineData("23505", "public", "OtherTemplates", "IX_AiPromptTemplates_Key_Locale", false)]
    [InlineData("23505", "other", "AiPromptTemplates", "IX_AiPromptTemplates_Key_Locale", false)]
    [InlineData("23503", "public", "AiPromptTemplates", "IX_AiPromptTemplates_Key_Locale", false)]
    public async Task SaveChangesAsync_TranslatesOnlyPromptIdentityConflict(
        string sqlState, string schema, string table, string constraint, bool isConflict) {
        var original = new DbUpdateException("Database failure", new PostgresException(
            "Database failure", "ERROR", "ERROR", sqlState,
            schemaName: schema, tableName: table, constraintName: constraint));
        DbContextOptions<AiDbContext> options = new DbContextOptionsBuilder<AiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new FailingSaveInterceptor(original)).Options;
        await using var context = new AiDbContext(options);

        Exception? exception = await Record.ExceptionAsync(() => context.SaveChangesAsync(CancellationToken.None));

        if (isConflict) {
            Assert.Same(original, Assert.IsType<DbUpdateConcurrencyException>(exception).InnerException);
        } else {
            Assert.Same(original, exception);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingSaveInterceptor(DbUpdateException exception) : SaveChangesInterceptor {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) => throw exception;
    }
}
