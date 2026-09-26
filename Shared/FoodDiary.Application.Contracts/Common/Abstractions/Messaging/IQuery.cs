using FoodDiary.Mediator;

namespace FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
