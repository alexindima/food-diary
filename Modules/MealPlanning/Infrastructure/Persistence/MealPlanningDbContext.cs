using FoodDiary.Modules.MealPlanning.PersistenceModel;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;

using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;

public sealed class MealPlanningDbContext(DbContextOptions<MealPlanningDbContext> options) : DbContext(options) {
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanDay> MealPlanDays => Set<MealPlanDay>();
    public DbSet<MealPlanMeal> MealPlanMeals => Set<MealPlanMeal>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ShoppingListItemSource> ShoppingListItemSources => Set<ShoppingListItemSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyMealPlanningPersistenceModel();
    }
}
