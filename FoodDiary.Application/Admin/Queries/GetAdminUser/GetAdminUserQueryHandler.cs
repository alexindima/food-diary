using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Queries.GetAdminUser;

public sealed class GetAdminUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminUserQuery, Result<AdminUserModel>> {
    public async Task<Result<AdminUserModel>> Handle(
        GetAdminUserQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<AdminUserModel>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        User? user = await userRepository.GetByIdIncludingDeletedAsync(new UserId(query.UserId), cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<AdminUserModel>(Errors.User.NotFound(query.UserId))
            : Result.Success(user.ToAdminModel());
    }
}
