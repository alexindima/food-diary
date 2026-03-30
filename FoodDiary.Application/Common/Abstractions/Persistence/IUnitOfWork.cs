namespace FoodDiary.Application.Common.Abstractions.Persistence;

public interface IUnitOfWork {
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
