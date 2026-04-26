using FoodDiary.Mediator;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
