using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.SkipCyclicDay;

public sealed record SkipCyclicDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
