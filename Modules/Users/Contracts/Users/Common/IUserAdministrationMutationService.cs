using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAdministrationMutationService {
    Task<Result<UserAdminReadModel>> CreateAsync(
        UserAdminCreateModel request,
        CancellationToken cancellationToken = default);

    Task<Result<UserAdminReadModel>> UpdateAsync(
        UserAdminUpdateModel request,
        CancellationToken cancellationToken = default);

    Task<Result> SetPasswordAsync(
        FoodDiary.Domain.ValueObjects.Ids.UserId userId,
        FoodDiary.Domain.ValueObjects.Ids.UserId actorUserId,
        string newPassword,
        CancellationToken cancellationToken = default);
}
