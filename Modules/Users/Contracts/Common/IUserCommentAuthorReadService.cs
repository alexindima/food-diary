using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserCommentAuthorReadService {
    Task<IReadOnlyDictionary<UserId, UserCommentAuthorModel>> GetAuthorsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default);
}
