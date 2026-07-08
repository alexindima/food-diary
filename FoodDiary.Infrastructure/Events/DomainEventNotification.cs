using FoodDiary.Domain.Primitives;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
