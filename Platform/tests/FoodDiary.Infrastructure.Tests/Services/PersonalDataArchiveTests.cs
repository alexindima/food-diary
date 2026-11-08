using System.IO.Compression;
using System.Text;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class PersonalDataArchiveTests {
    [Fact]
    public async Task Archive_ContainsOwnRecordsAndNeverSerializesCredentialsOrForeignAccounts() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new FoodDiaryDbContext(options);
        var owner = User.Create("archive-owner@example.test", "password-hash-must-not-be-exported");
        var stranger = User.Create("foreign-account-must-not-be-exported@example.test", "foreign-hash");
        context.Users.AddRange(owner, stranger);
        context.Set<UserActivity>().AddRange(
            UserActivity.Create(owner.Id, new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc)),
            UserActivity.Create(stranger.Id, new DateTime(2031, 1, 2, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddSingleton<ICompositionReadContext>(context);
        services.AddSingleton(TimeProvider.System);
        services.AddReadModelComposition();
        await using ServiceProvider provider = services.BuildServiceProvider();

        byte[] bytes = await provider.GetRequiredService<IPersonalDataArchiveReader>().ReadArchiveAsync(owner.Id, CancellationToken.None);

        await using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("manifest.json"));
        Assert.NotNull(zip.GetEntry("UserPrivacyConsentEvent.json"));
        using var text = new StreamReader(await zip.GetEntry("User.json")!.OpenAsync(CancellationToken.None), Encoding.UTF8);
        string json = await text.ReadToEndAsync(CancellationToken.None);
        Assert.Contains("archive-owner@example.test", json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("foreign-account", json, StringComparison.Ordinal);
        Assert.Contains("2030-01-02T12:00:00Z", json, StringComparison.Ordinal);
        Assert.DoesNotContain("2031-01-02", json, StringComparison.Ordinal);
        Assert.Null(zip.GetEntry("FertilitySignal.json"));
    }
}
