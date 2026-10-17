using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Identity.PersistenceModel.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramAssertionPolicy {
    public int Id { get; init; }
    public DateTime NotBeforeUtc { get; set; }
}
