using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserCommentAuthorReadService {
    Task<IReadOnlyDictionary<UserId, UserCommentAuthorModel>> GetAuthorsAsync(
        IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default);
}
