using MediatR;

namespace FoodDiary.Application.Common.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
