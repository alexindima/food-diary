using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects;

/// <summary>
/// Строготипизированный идентификатор пользователя
/// </summary>
public readonly record struct UserId(Guid Value) : IEntityId<Guid>
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);

    public static implicit operator Guid(UserId id) => id.Value;
    public static explicit operator UserId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор продукта
/// </summary>
public readonly record struct ProductId(Guid Value) : IEntityId<Guid>
{
    public static ProductId New() => new(Guid.NewGuid());
    public static ProductId Empty => new(Guid.Empty);

    public static implicit operator Guid(ProductId id) => id.Value;
    public static explicit operator ProductId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор приема пищи
/// </summary>
public readonly record struct MealId(Guid Value) : IEntityId<Guid>
{
    public static MealId New() => new(Guid.NewGuid());
    public static MealId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealId id) => id.Value;
    public static explicit operator MealId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор рецепта
/// </summary>
public readonly record struct RecipeId(Guid Value) : IEntityId<Guid>
{
    public static RecipeId New() => new(Guid.NewGuid());
    public static RecipeId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeId id) => id.Value;
    public static explicit operator RecipeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор элемента приема пищи
/// </summary>
public readonly record struct MealItemId(Guid Value) : IEntityId<Guid>
{
    public static MealItemId New() => new(Guid.NewGuid());
    public static MealItemId Empty => new(Guid.Empty);

    public static implicit operator Guid(MealItemId id) => id.Value;
    public static explicit operator MealItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор шага рецепта
/// </summary>
public readonly record struct RecipeStepId(Guid Value) : IEntityId<Guid>
{
    public static RecipeStepId New() => new(Guid.NewGuid());
    public static RecipeStepId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeStepId id) => id.Value;
    public static explicit operator RecipeStepId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор ингредиента рецепта
/// </summary>
public readonly record struct RecipeIngredientId(Guid Value) : IEntityId<Guid>
{
    public static RecipeIngredientId New() => new(Guid.NewGuid());
    public static RecipeIngredientId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecipeIngredientId id) => id.Value;
    public static explicit operator RecipeIngredientId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Строготипизированный идентификатор записи веса
/// </summary>
public readonly record struct WeightEntryId(Guid Value) : IEntityId<Guid>
{
    public static WeightEntryId New() => new(Guid.NewGuid());
    public static WeightEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(WeightEntryId id) => id.Value;
    public static explicit operator WeightEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identifier for waist circumference entries.
/// </summary>
public readonly record struct WaistEntryId(Guid Value) : IEntityId<Guid>
{
    public static WaistEntryId New() => new(Guid.NewGuid());
    public static WaistEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(WaistEntryId id) => id.Value;
    public static explicit operator WaistEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identifier for menstrual cycles.
/// </summary>
public readonly record struct CycleId(Guid Value) : IEntityId<Guid>
{
    public static CycleId New() => new(Guid.NewGuid());
    public static CycleId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleId id) => id.Value;
    public static explicit operator CycleId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identifier for cycle days.
/// </summary>
public readonly record struct CycleDayId(Guid Value) : IEntityId<Guid>
{
    public static CycleDayId New() => new(Guid.NewGuid());
    public static CycleDayId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleDayId id) => id.Value;
    public static explicit operator CycleDayId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identifier for stored images.
/// </summary>
public readonly record struct ImageAssetId(Guid Value) : IEntityId<Guid>
{
    public static ImageAssetId New() => new(Guid.NewGuid());
    public static ImageAssetId Empty => new(Guid.Empty);

    public static implicit operator Guid(ImageAssetId id) => id.Value;
    public static explicit operator ImageAssetId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identifier for hydration entries.
/// </summary>
public readonly record struct HydrationEntryId(Guid Value) : IEntityId<Guid>
{
    public static HydrationEntryId New() => new(Guid.NewGuid());
    public static HydrationEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(HydrationEntryId id) => id.Value;
    public static explicit operator HydrationEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
