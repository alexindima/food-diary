using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductDescriptiveIdentityChanges(
    FieldChange<string> Category,
    FieldChange<string> Description,
    FieldChange<string> Comment);
