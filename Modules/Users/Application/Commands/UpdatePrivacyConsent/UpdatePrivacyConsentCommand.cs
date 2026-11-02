using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.UpdatePrivacyConsent;

public sealed record UpdatePrivacyConsentCommand(Guid? UserId, int Purpose, bool Granted, string NoticeVersion) : ICommand<Result>, IUserRequest;
