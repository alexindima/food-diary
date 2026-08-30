using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery(Guid? UserId) : IQuery<Result<int>>, IUserRequest;
