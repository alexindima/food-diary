using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class FoodDiaryDbContextFactory : IDesignTimeDbContextFactory<FoodDiaryDbContext> {
    public FoodDiaryDbContext CreateDbContext(string[] args) {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("70b9cd39-ddfa-4d58-9b62-3a8a55010f7d")
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection") ??
            Environment.GetEnvironmentVariable("FOODDIARY_CONNECTION_STRING") ??
            "Host=localhost;Database=food_diary;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<FoodDiaryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FoodDiaryDbContext(optionsBuilder.Options);
    }
}
