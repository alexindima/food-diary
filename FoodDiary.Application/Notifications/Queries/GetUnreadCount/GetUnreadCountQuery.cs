using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery(Guid? UserId) : IQuery<Result<int>>, IUserRequest;
