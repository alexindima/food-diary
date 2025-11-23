using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class FoodDiaryDbContext : DbContext
{
    public FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<CycleDay> CycleDays => Set<CycleDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ActivityLevel)
                .HasConversion<string>()
                .HasDefaultValue(ActivityLevel.Moderate);

            entity.HasMany(e => e.WeightEntries)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.WaistEntries)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new ProductId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Visibility).HasDefaultValue(Visibility.PUBLIC);
            entity.Property(e => e.ProductType).HasDefaultValue(ProductType.Unknown);

            // UsageCount - не хранится в БД, вычисляется динамически в запросах
            entity.Ignore(e => e.UsageCount);

            entity.HasOne(e => e.User).WithMany(u => u.Products).HasForeignKey(e => e.UserId);
        });

        // Meal configuration
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new MealId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.HasOne(e => e.User).WithMany(u => u.Meals).HasForeignKey(e => e.UserId);
            entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);
        });

        // Recipe configuration
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new RecipeId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Visibility).HasDefaultValue(Visibility.PUBLIC);
            entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);

            entity.Ignore(e => e.UsageCount);

            entity.HasOne(e => e.User).WithMany(u => u.Recipes).HasForeignKey(e => e.UserId);

            entity.HasMany(e => e.MealItems)
                .WithOne(mi => mi.Recipe)
                .HasForeignKey(mi => mi.RecipeId)
                .IsRequired(false);
        });

        // MealItem configuration - XOR constraint: ProductId OR RecipeId
        modelBuilder.Entity<MealItem>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new MealItemId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.MealId).HasConversion(
                id => id.Value,
                value => new MealId(value));

            entity.Property(e => e.ProductId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ProductId(value.Value) : null);

            entity.Property(e => e.RecipeId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new RecipeId(value.Value) : null);

            entity.HasOne(e => e.Meal)
                .WithMany(m => m.Items)
                .HasForeignKey(e => e.MealId);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.MealItems)
                .HasForeignKey(e => e.ProductId)
                .IsRequired(false);

            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.MealItems)
                .HasForeignKey(e => e.RecipeId)
                .IsRequired(false);

            // XOR check constraint будет в миграции: CHECK ((ProductId IS NULL) <> (RecipeId IS NULL))
        });

        // RecipeStep configuration
        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new RecipeStepId(value));

            entity.Property(e => e.RecipeId).HasConversion(
                id => id.Value,
                value => new RecipeId(value));

            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.Steps)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new RecipeIngredientId(value));

            entity.Property(e => e.ProductId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ProductId(value.Value) : null);

            entity.Property(e => e.RecipeStepId).HasConversion(
                id => id.Value,
                value => new RecipeStepId(value));

            entity.Property(e => e.NestedRecipeId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new RecipeId(value.Value) : null);

            entity.HasOne(e => e.RecipeStep)
                .WithMany(s => s.Ingredients)
                .HasForeignKey(e => e.RecipeStepId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.RecipeIngredients)
                .HasForeignKey(e => e.ProductId)
                .IsRequired(false);

            entity.HasOne(e => e.NestedRecipe)
                .WithMany(r => r.NestedRecipeUsages)
                .HasForeignKey(e => e.NestedRecipeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeightEntry>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new WeightEntryId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Date)
                .HasColumnType("date");

            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.WeightEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WaistEntry>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new WaistEntryId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Date)
                .HasColumnType("date");

            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.WaistEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cycle>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new CycleId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.StartDate)
                .HasColumnType("date");

            entity.Property(e => e.AverageLength)
                .HasDefaultValue(28);

            entity.Property(e => e.LutealLength)
                .HasDefaultValue(14);

            entity.Property(e => e.Notes)
                .HasMaxLength(1024);

            entity.HasMany(e => e.Days)
                .WithOne(d => d.Cycle)
                .HasForeignKey(d => d.CycleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Days)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasIndex(e => new { e.UserId, e.StartDate })
                .HasDatabaseName("IX_Cycles_User_StartDate");
        });

        modelBuilder.Entity<CycleDay>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new CycleDayId(value));

            entity.Property(e => e.CycleId).HasConversion(
                id => id.Value,
                value => new CycleId(value));

            entity.Property(e => e.Date)
                .HasColumnType("date");

            entity.Property(e => e.Notes)
                .HasMaxLength(1024);

            entity.HasIndex(e => new { e.CycleId, e.Date }).IsUnique();

            entity.OwnsOne(e => e.Symptoms, builder =>
            {
                builder.Property(s => s.Pain).HasColumnName("Pain").IsRequired();
                builder.Property(s => s.Mood).HasColumnName("Mood").IsRequired();
                builder.Property(s => s.Edema).HasColumnName("Edema").IsRequired();
                builder.Property(s => s.Headache).HasColumnName("Headache").IsRequired();
                builder.Property(s => s.Energy).HasColumnName("Energy").IsRequired();
                builder.Property(s => s.SleepQuality).HasColumnName("SleepQuality").IsRequired();
                builder.Property(s => s.Libido).HasColumnName("Libido").IsRequired();
            });
        });
    }
}
