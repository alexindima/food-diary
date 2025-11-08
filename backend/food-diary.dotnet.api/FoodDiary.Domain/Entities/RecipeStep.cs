using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Шаг рецепта - часть агрегата Recipe
/// НЕ является корнем агрегата
/// </summary>
public class RecipeStep : Entity<int>
{
    public int RecipeId { get; private set; }
    public int StepNumber { get; private set; }
    public string Instruction { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }

    // Navigation properties
    public virtual Recipe Recipe { get; private set; } = null!;

    // Конструктор для EF Core
    private RecipeStep() { }

    // Factory method (вызывается из Recipe агрегата)
    internal static RecipeStep Create(int recipeId, int stepNumber, string instruction, string? imageUrl = null)
    {
        var step = new RecipeStep
        {
            RecipeId = recipeId,
            StepNumber = stepNumber,
            Instruction = instruction,
            ImageUrl = imageUrl
        };
        step.SetCreated();
        return step;
    }

    public void Update(string instruction, string? imageUrl = null)
    {
        Instruction = instruction;
        ImageUrl = imageUrl;
        SetModified();
    }
}
