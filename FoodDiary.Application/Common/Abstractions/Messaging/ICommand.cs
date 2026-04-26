using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>;
