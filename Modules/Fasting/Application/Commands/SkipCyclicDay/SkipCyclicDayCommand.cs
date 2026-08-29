using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.SkipCyclicDay;

public sealed record SkipCyclicDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
