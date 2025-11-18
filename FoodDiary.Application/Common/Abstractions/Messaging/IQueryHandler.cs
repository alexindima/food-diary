using MediatR;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

/// <summary>
/// Представляет обработчик запроса
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
