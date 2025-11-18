using System;

namespace FoodDiary.Application.Consumptions.Common;

public record ConsumptionItemInput(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount);
