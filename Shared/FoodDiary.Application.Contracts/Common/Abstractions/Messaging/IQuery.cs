using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
