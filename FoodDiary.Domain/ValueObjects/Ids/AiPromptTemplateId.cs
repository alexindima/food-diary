using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct AiPromptTemplateId(Guid Value) : IEntityId<Guid> {
    public static AiPromptTemplateId New() => new(Guid.NewGuid());
    public static AiPromptTemplateId Empty => new(Guid.Empty);

    public static implicit operator Guid(AiPromptTemplateId id) => id.Value;
    public static explicit operator AiPromptTemplateId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
