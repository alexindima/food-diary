using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.RecordUserActivity;

public sealed record RecordUserActivityCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
