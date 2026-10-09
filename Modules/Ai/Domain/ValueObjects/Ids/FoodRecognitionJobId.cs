using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;

public readonly record struct FoodRecognitionJobId(Guid Value) : IEntityId<Guid> {
    public override string ToString() => Value.ToString();
}
