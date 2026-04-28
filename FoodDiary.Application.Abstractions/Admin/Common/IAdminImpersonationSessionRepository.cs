using FoodDiary.Domain.Entities.Admin;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminImpersonationSessionRepository {
    Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default);
}
