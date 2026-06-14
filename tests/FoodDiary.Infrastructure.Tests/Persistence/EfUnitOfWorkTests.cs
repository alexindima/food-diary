using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class EfUnitOfWorkTests {
    [Fact]
    public async Task HasPendingChanges_ReflectsChangeTrackerAndSaveChangesPersistsChanges() {
        await using FoodDiaryDbContext context = CreateContext();
        var unitOfWork = new EfUnitOfWork(context);
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        context.ShoppingLists.Add(list);

        Assert.True(unitOfWork.HasPendingChanges);

        await unitOfWork.SaveChangesAsync();

        Assert.False(unitOfWork.HasPendingChanges);
        Assert.NotNull(await context.ShoppingLists.FindAsync(list.Id));
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
