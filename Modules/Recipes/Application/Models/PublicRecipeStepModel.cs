using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Application.Models;

[ExcludeFromCodeCoverage]
public sealed record PublicRecipeStepModel(int StepNumber, string? Title, string Instruction,
    IReadOnlyList<string> Images, IReadOnlyList<PublicRecipeIngredientModel> Ingredients);
