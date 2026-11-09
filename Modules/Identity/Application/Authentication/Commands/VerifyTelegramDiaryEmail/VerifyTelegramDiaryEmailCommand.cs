using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyTelegramDiaryEmail;

public sealed record VerifyTelegramDiaryEmailCommand(Guid UserId, string Token, string BrowserBinding)
    : ICommand<Result<TelegramDiaryEmailVerification>>, IAtomicCommand;
