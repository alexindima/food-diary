using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class FoodDiaryDbContextFactory : IDesignTimeDbContextFactory<FoodDiaryDbContext> {
    public FoodDiaryDbContext CreateDbContext(string[] args) {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("FOODDIARY_CONNECTION_STRING") ??
            "Host=localhost;Database=food_diary;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<FoodDiaryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FoodDiaryDbContext(optionsBuilder.Options);
    }
}
