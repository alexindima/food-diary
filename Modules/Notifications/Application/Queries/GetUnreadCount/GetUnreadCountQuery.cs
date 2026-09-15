using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetUnreadCount;

public record GetUnreadCountQuery(Guid? UserId) : IQuery<Result<int>>, IUserRequest;
