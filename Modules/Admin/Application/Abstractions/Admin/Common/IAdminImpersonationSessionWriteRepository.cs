using FoodDiary.Domain.Entities.Admin;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminImpersonationSessionWriteRepository {
    Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default);
}
