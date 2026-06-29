using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Shopping;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public partial class FoodDiaryDbContext {
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<MealAiSession> MealAiSessions => Set<MealAiSession>();
    public DbSet<MealAiItem> MealAiItems => Set<MealAiItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ShoppingListItemSource> ShoppingListItemSources => Set<ShoppingListItemSource>();
    public DbSet<FavoriteMeal> FavoriteMeals => Set<FavoriteMeal>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanDay> MealPlanDays => Set<MealPlanDay>();
    public DbSet<MealPlanMeal> MealPlanMeals => Set<MealPlanMeal>();
}
