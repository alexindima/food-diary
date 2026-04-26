using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed record SkipCyclicDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
