using FoodDiary.Domain.Primitives;
using FoodDiary.Mediator;

namespace FoodDiary.Persistence.Runtime.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
