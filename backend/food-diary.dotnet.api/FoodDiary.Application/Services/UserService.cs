using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> GetUserByIdAsync(UserId userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        return MapToUserResponse(user);
    }

    public async Task<UserResponse> UpdateUserAsync(UserId userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.UpdateProfile(
            username: request.Username,
            firstName: request.FirstName,
            lastName: request.LastName,
            birthDate: request.BirthDate,
            gender: request.Gender,
            weight: request.Weight,
            height: request.Height,
            profileImage: request.ProfileImage
        );

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        // TODO: Добавить метод для изменения пароля в User entity
        // if (request.Password != null)
        // {
        //     user.UpdatePassword(_passwordHasher.Hash(request.Password));
        // }

        await _userRepository.UpdateAsync(user);

        return MapToUserResponse(user);
    }

    private static UserResponse MapToUserResponse(User user)
    {
        return new UserResponse(
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.BirthDate,
            user.Gender,
            user.Weight,
            user.Height,
            user.ProfileImage,
            user.IsActive
        );
    }
}
