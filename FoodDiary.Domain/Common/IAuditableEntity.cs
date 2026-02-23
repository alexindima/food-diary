namespace FoodDiary.Domain.Common;

public interface IAuditableEntity {
    DateTime CreatedOnUtc { get; }
    DateTime? ModifiedOnUtc { get; }
}
