using FoodDiary.Domain.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
