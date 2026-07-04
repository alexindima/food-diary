using FoodDiary.Domain.Entities.FavoriteProducts;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
}
