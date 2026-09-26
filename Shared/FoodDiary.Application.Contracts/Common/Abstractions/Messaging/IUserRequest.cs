namespace FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

public interface IUserRequest {
    Guid? UserId { get; }
}
