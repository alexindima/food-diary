using FoodDiary.Mediator;

namespace FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>, ITransactionalCommand;
