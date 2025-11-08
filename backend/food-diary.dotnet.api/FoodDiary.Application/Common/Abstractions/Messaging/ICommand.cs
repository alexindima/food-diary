using MediatR;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

/// <summary>
/// Представляет команду с результатом
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
