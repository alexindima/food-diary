namespace FoodDiary.Application.Common.Abstractions.Messaging;

public interface IUserRequest {
    Guid? UserId { get; }
}
