using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
