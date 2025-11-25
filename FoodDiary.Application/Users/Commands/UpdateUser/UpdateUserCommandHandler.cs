using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateUserCommand, Result<UserResponse>> {
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null) {
            return Result.Failure<UserResponse>(User.NotFound(command.UserId.Value));
        }

        var activityLevelResult = ParseActivityLevel(command.ActivityLevel);
        if (activityLevelResult.IsFailure)
        {
            return Result.Failure<UserResponse>(activityLevelResult.Error);
        }

        user.UpdateProfile(
            username: Normalize(command.Username),
            firstName: Normalize(command.FirstName),
            lastName: Normalize(command.LastName),
            birthDate: command.BirthDate,
            gender: Normalize(command.Gender),
            weight: command.Weight,
            height: command.Height,
            activityLevel: activityLevelResult.Value,
            dailyCalorieTarget: command.DailyCalorieTarget,
            proteinTarget: command.ProteinTarget,
            fatTarget: command.FatTarget,
            carbTarget: command.CarbTarget,
            fiberTarget: command.FiberTarget,
            stepGoal: command.StepGoal,
            waterGoal: command.WaterGoal,
            profileImage: Normalize(command.ProfileImage)
        );

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        await userRepository.UpdateAsync(user);

        return Result.Success(user.ToResponse());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result<ActivityLevel?> ParseActivityLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<ActivityLevel?>(null);
        }

        return Enum.TryParse<ActivityLevel>(value, true, out var parsed)
            ? Result.Success<ActivityLevel?>(parsed)
            : Result.Failure<ActivityLevel?>(Validation.Invalid(nameof(UpdateUserCommand.ActivityLevel), "Invalid activity level value."));
    }
}
