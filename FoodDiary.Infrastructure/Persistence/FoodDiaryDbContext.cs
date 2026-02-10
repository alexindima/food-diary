using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class FoodDiaryDbContext : DbContext
{
    public FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<MealAiSession> MealAiSessions => Set<MealAiSession>();
    public DbSet<MealAiItem> MealAiItems => Set<MealAiItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<CycleDay> CycleDays => Set<CycleDay>();
    public DbSet<HydrationEntry> HydrationEntries => Set<HydrationEntry>();
    public DbSet<DailyAdvice> DailyAdvices => Set<DailyAdvice>();
    public DbSet<AiUsage> AiUsages => Set<AiUsage>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.ProfileImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEmailConfirmed).HasDefaultValue(false);
            entity.Property(e => e.EmailConfirmationTokenExpiresAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.EmailConfirmationSentAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.PasswordResetTokenExpiresAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.PasswordResetSentAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.LastLoginAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp with time zone");
            entity.Property(e => e.ActivityLevel)
                .HasConversion<string>()
                .HasDefaultValue(ActivityLevel.Moderate);
            entity.Property(e => e.Language)
                .HasDefaultValue("en");
            entity.Property(e => e.TelegramUserId)
                .HasColumnType("bigint");
            entity.Property(e => e.DashboardLayoutJson)
                .HasColumnType("jsonb")
                .HasColumnName("DashboardLayout");
            entity.Property(e => e.AiInputTokenLimit)
                .HasDefaultValue(5_000_000L);
            entity.Property(e => e.AiOutputTokenLimit)
                .HasDefaultValue(1_000_000L);

            entity.HasMany(e => e.WeightEntries)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.WaistEntries)
                .WithOne(w => w.User)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.HydrationEntries)
                .WithOne(h => h.User)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ImageAsset>()
                .WithMany()
                .HasForeignKey(e => e.ProfileImageAssetId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.TelegramUserId)
                .IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new RoleId(value));

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.RoleId).HasConversion(
                id => id.Value,
                value => new RoleId(value));

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImageAsset>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new ImageAssetId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.ObjectKey).IsRequired();
            entity.Property(e => e.Url).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
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

            entity.Property(e => e.ImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.Property(e => e.Visibility).HasDefaultValue(Visibility.PUBLIC);
            entity.Property(e => e.ProductType).HasDefaultValue(ProductType.Unknown);

            // UsageCount - не хранится в БД, вычисляется динамически в запросах
            entity.Ignore(e => e.UsageCount);

            entity.HasOne<ImageAsset>()
                .WithMany()
                .HasForeignKey(e => e.ImageAssetId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

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

            entity.Property(e => e.ImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.HasOne(e => e.User).WithMany(u => u.Meals).HasForeignKey(e => e.UserId);
            entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);

            entity.HasOne<ImageAsset>()
                .WithMany()
                .HasForeignKey(e => e.ImageAssetId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.AiSessions)
                .WithOne(s => s.Meal)
                .HasForeignKey(s => s.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.AiSessions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
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

            entity.Property(e => e.ImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.Property(e => e.Visibility).HasDefaultValue(Visibility.PUBLIC);
            entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);

            entity.Ignore(e => e.UsageCount);

            entity.HasOne(e => e.User).WithMany(u => u.Recipes).HasForeignKey(e => e.UserId);

            entity.HasMany(e => e.MealItems)
                .WithOne(mi => mi.Recipe)
                .HasForeignKey(mi => mi.RecipeId)
                .IsRequired(false);

            entity.HasOne<ImageAsset>()
                .WithMany()
                .HasForeignKey(e => e.ImageAssetId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<MealAiSession>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new MealAiSessionId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.MealId).HasConversion(
                id => id.Value,
                value => new MealId(value));

            entity.Property(e => e.ImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.HasOne(e => e.ImageAsset)
                .WithMany()
                .HasForeignKey(e => e.ImageAssetId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.RecognizedAtUtc)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.Notes)
                .HasMaxLength(2048);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.Session)
                .HasForeignKey(i => i.MealAiSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<MealAiItem>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new MealAiItemId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.MealAiSessionId).HasConversion(
                id => id.Value,
                value => new MealAiSessionId(value));

            entity.Property(e => e.NameEn)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.NameLocal)
                .HasMaxLength(256);

            entity.Property(e => e.Unit)
                .IsRequired()
                .HasMaxLength(32);
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

            entity.Property(e => e.ImageAssetId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ImageAssetId(value.Value) : null);

            entity.HasOne<ImageAsset>()
                .WithMany()
                .HasForeignKey(e => e.ImageAssetId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ShoppingListId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(128);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ShoppingLists)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.ShoppingList)
                .HasForeignKey(i => i.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(e => e.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    id => id.Value,
                    value => new ShoppingListItemId(value))
                .ValueGeneratedNever();

            entity.Property(e => e.ShoppingListId).HasConversion(
                id => id.Value,
                value => new ShoppingListId(value));

            entity.Property(e => e.ProductId).HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ProductId(value.Value) : null);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Category)
                .HasMaxLength(128);

            entity.Property(e => e.IsChecked)
                .HasDefaultValue(false);

            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<HydrationEntry>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new HydrationEntryId(value));

            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.AmountMl)
                .IsRequired();

            entity.HasIndex(e => new { e.UserId, e.Timestamp }).HasDatabaseName("IX_HydrationEntries_User_Timestamp");

            entity.HasOne(e => e.User)
                .WithMany(u => u.HydrationEntries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyAdvice>(entity =>
        {
            entity.Property(e => e.Id).HasConversion(
                id => id.Value,
                value => new DailyAdviceId(value));

            entity.Property(e => e.Locale)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.Tag)
                .HasMaxLength(64);

            entity.Property(e => e.Weight)
                .HasDefaultValue(1);

            entity.HasIndex(e => new { e.Locale, e.Tag });
        });

        modelBuilder.Entity<AiUsage>(entity =>
        {
            entity.Property(e => e.UserId).HasConversion(
                id => id.Value,
                value => new UserId(value));

            entity.Property(e => e.Operation)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(e => e.Model)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedOnUtc);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Locale)
                .IsRequired()
                .HasMaxLength(8);

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.HtmlBody)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.TextBody)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(e => new { e.Key, e.Locale })
                .IsUnique();
        });
    }
}
