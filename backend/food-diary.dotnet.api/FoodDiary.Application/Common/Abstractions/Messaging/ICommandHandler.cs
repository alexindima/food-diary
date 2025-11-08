using MediatR;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

/// <summary>
/// Представляет обработчик команды
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
