using FoodDiary.Domain.Entities.Admin;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminImpersonationSessionWriteRepository {
    Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default);
}
