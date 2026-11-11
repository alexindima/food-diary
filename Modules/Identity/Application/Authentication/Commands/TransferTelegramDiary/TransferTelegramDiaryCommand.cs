using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;

public sealed record TransferTelegramDiaryCommand(Guid UserId, string Ticket, string BrowserBinding, bool? UseTelegramGoals, Guid SessionId = default)
    : ICommand<Result<TelegramDiaryPreview>>, IAtomicCommand;
