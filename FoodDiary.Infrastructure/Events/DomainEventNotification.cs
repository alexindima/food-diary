using FoodDiary.Domain.Common;
using MediatR;

namespace FoodDiary.Infrastructure.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
