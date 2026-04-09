using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicFastDay;

public sealed record PostponeCyclicFastDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
