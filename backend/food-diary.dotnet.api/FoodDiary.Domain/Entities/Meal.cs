using System;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Прием пищи - корень агрегата
/// Управляет коллекцией MealItems (продуктов и блюд)
/// </summary>
public sealed class Meal : AggregateRoot<int> {
    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; } = DateTime.UtcNow;
    public MealType? MealType { get; private set; }
    public string? Comment { get; private set; }
    public double TotalCalories { get; private set; }
    public double TotalProteins { get; private set; }
    public double TotalFats { get; private set; }
    public double TotalCarbs { get; private set; }
    public double TotalFiber { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    private readonly List<MealItem> _items = new();
    public IReadOnlyCollection<MealItem> Items => _items.AsReadOnly();

    // Конструктор для EF Core
    private Meal() {
        _items = new List<MealItem>();
    }

    // Factory method для создания приема пищи
    public static Meal Create(
        UserId userId,
        DateTime date,
        MealType? mealType = null,
        string? comment = null) {
        var meal = new Meal {
            UserId = userId,
            Date = date,
            MealType = mealType,
            Comment = comment
        };
        meal.SetCreated();
        return meal;
    }

    public void UpdateComment(string? comment) {
        Comment = comment;
        SetModified();
    }

    public void UpdateDate(DateTime date) {
        Date = date;
        SetModified();
    }

    public void UpdateMealType(MealType? mealType) {
        MealType = mealType;
        SetModified();
    }

    /// <summary>
    /// Добавить продукт в прием пищи
    /// </summary>
    public MealItem AddProduct(ProductId productId, double amount) {
        var item = MealItem.CreateWithProduct(Id, productId, amount);
        _items.Add(item);
        SetModified();
        return item;
    }

    /// <summary>
    /// Добавить блюдо (рецепт) в прием пищи
    /// </summary>
    public MealItem AddRecipe(RecipeId recipeId, double servings) {
        var item = MealItem.CreateWithRecipe(Id, recipeId, servings);
        _items.Add(item);
        SetModified();
        return item;
    }

    public void RemoveItem(MealItem item) {
        _items.Remove(item);
        SetModified();
    }

    public void ClearItems() {
        _items.Clear();
        SetModified();
    }

    public void ApplyNutrition(
        double totalCalories,
        double totalProteins,
        double totalFats,
        double totalCarbs,
        double totalFiber) {
        TotalCalories = Math.Round(totalCalories, 2);
        TotalProteins = Math.Round(totalProteins, 2);
        TotalFats = Math.Round(totalFats, 2);
        TotalCarbs = Math.Round(totalCarbs, 2);
        TotalFiber = Math.Round(totalFiber, 2);
        SetModified();
    }
}
