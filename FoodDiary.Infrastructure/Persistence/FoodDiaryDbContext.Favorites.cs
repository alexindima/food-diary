using FoodDiary.Domain.Entities.FavoriteProducts;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public partial class FoodDiaryDbContext {
    public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
}
