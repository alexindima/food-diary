using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.PostponeCyclicDay;

public sealed record PostponeCyclicDayCommand(Guid? UserId) : ICommand<Result<FastingSessionModel>>;
