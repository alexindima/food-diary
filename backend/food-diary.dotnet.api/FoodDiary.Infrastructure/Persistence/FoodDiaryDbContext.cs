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

            // UsageCount - не хранится в БД, вычисляется динамически в запросах
            entity.Ignore(e => e.UsageCount);

            entity.HasOne(e => e.User).WithMany(u => u.Products).HasForeignKey(e => e.UserId);
        });

        // Meal configuration
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.HasOne(e => e.User).WithMany(u => u.Meals).HasForeignKey(e => e.UserId);
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
    }
}
