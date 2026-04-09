using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicFastDay;

public sealed record SkipCyclicFastDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
