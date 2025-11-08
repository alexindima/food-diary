using MediatR;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

/// <summary>
/// Представляет запрос на получение данных
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
