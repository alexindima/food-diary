namespace FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

public interface IUserRequest {
    Guid? UserId { get; }
}
