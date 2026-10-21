using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class UserProfileWriteGuardProviderBoundaryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedContextOrProvider_SavesNormallyWithoutRequiringTheOwnerTransaction(bool foreignContext) {
        string databaseName = Guid.NewGuid().ToString("N");
        var interceptor = new UserProfileWriteGuardInterceptor();
        await using DbContext context = foreignContext
            ? new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(databaseName).AddInterceptors(interceptor).Options)
            : new UsersDbContext(new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(databaseName).AddInterceptors(interceptor).Options);
        var user = User.Create("guard-provider@example.test", "hash");
        user.SetLanguage("ru");
        context.Add(user);

        Assert.True(await context.SaveChangesAsync() > 0);

        User persisted = await context.Set<User>().AsNoTracking().SingleAsync();
        Assert.Equal(user.Id, persisted.Id);
        Assert.Equal("ru", persisted.Language);
        Assert.Equal(0, await context.SaveChangesAsync());
    }
}
