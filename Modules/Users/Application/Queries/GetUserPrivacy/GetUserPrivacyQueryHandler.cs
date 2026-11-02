using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserPrivacy;

public sealed class GetUserPrivacyQueryHandler(ICurrentUserAccessService access, IUserPrivacyStateReader reader)
    : IQueryHandler<GetUserPrivacyQuery, Result<UserPrivacyStateModel>> {
    public async Task<Result<UserPrivacyStateModel>> Handle(GetUserPrivacyQuery query, CancellationToken cancellationToken) {
        Result<UserId> id = await CurrentUserAccessResolver.ResolveAsync(query.UserId, access, cancellationToken);
        if (id.IsFailure) { return Result.Failure<UserPrivacyStateModel>(id.Error); }
        UserPrivacyStateModel? state = await reader.GetAsync(id.Value, cancellationToken);
        return state is null ? Result.Failure<UserPrivacyStateModel>(UserErrors.NotFound(id.Value.Value)) : Result.Success(state);
    }
}
