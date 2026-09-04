using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactionalCommand;
