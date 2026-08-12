using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;

public sealed record PostponeCyclicDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
