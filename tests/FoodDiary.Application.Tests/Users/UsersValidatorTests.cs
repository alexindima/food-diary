using FluentValidation.TestHelper;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Commands.UpdateUserAppearance;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Queries.GetUserGoals;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public class UsersValidatorTests {
    // â”€â”€ ChangePassword â”€â”€

    [Fact]
    public async Task ChangePassword_WithNullUserId_HasError() {
        var v = new ChangePasswordCommandValidator();
        var result = await v.TestValidateAsync(new ChangePasswordCommand(null, "old", "newpass"));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task ChangePassword_WithEmptyCurrentPassword_HasError() {
        var v = new ChangePasswordCommandValidator();
        var result = await v.TestValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "", "newpass"));
        result.ShouldHaveValidationErrorFor(c => c.CurrentPassword);
    }

    [Fact]
    public async Task ChangePassword_WithShortNewPassword_HasError() {
        var v = new ChangePasswordCommandValidator();
        var result = await v.TestValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "oldpass", "12345"));
        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public async Task ChangePassword_WithSamePasswords_HasError() {
        var v = new ChangePasswordCommandValidator();
        var result = await v.TestValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "samepass", "samepass"));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_NoErrors() {
        var v = new ChangePasswordCommandValidator();
        var result = await v.TestValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "oldpass", "newpass"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ DeleteUser â”€â”€

    [Fact]
    public async Task DeleteUser_WithNullUserId_HasError() {
        var v = new DeleteUserCommandValidator();
        var result = await v.TestValidateAsync(new DeleteUserCommand(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // â”€â”€ UpdateDesiredWaist â”€â”€

    [Fact]
    public async Task UpdateDesiredWaist_WithNullUserId_HasError() {
        var v = new UpdateDesiredWaistCommandValidator();
        var result = await v.TestValidateAsync(new UpdateDesiredWaistCommand(null, 80));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateDesiredWaist_WithZeroValue_HasError() {
        var v = new UpdateDesiredWaistCommandValidator();
        var result = await v.TestValidateAsync(new UpdateDesiredWaistCommand(Guid.NewGuid(), 0));
        result.ShouldHaveValidationErrorFor(c => c.DesiredWaist);
    }

    [Fact]
    public async Task UpdateDesiredWaist_WithNull_NoErrors() {
        var v = new UpdateDesiredWaistCommandValidator();
        var result = await v.TestValidateAsync(new UpdateDesiredWaistCommand(Guid.NewGuid(), null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ UpdateDesiredWeight â”€â”€

    [Fact]
    public async Task UpdateDesiredWeight_WithZeroValue_HasError() {
        var v = new UpdateDesiredWeightCommandValidator();
        var result = await v.TestValidateAsync(new UpdateDesiredWeightCommand(Guid.NewGuid(), 0));
        result.ShouldHaveValidationErrorFor(c => c.DesiredWeight);
    }

    // â”€â”€ UpdateUser â”€â”€

    [Fact]
    public async Task UpdateUser_WithNullUserId_HasError() {
        var v = new UpdateUserCommandValidator();
        var result = await v.TestValidateAsync(new UpdateUserCommand(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateUser_WithNegativeWeight_HasError() {
        var v = new UpdateUserCommandValidator();
        var result = await v.TestValidateAsync(new UpdateUserCommand(Guid.NewGuid(), null, null, null, null, null, -1, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.Weight);
    }

    [Fact]
    public async Task SetPassword_WithNullUserId_HasError() {
        var result = await new SetPasswordCommandValidator().TestValidateAsync(
            new SetPasswordCommand(null, "new-password"));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task SetPassword_WithShortPassword_HasError() {
        var result = await new SetPasswordCommandValidator().TestValidateAsync(
            new SetPasswordCommand(Guid.NewGuid(), "12345"));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public async Task SetPassword_WithValidCommand_HasNoErrors() {
        var result = await new SetPasswordCommandValidator().TestValidateAsync(
            new SetPasswordCommand(Guid.NewGuid(), "new-password"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateUserAppearance_WithNullUserId_HasError() {
        var result = await new UpdateUserAppearanceCommandValidator().TestValidateAsync(
            new UpdateUserAppearanceCommand(null, "dark", null));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateUserAppearance_WithNoAppearanceFields_HasError() {
        var result = await new UpdateUserAppearanceCommandValidator().TestValidateAsync(
            new UpdateUserAppearanceCommand(Guid.NewGuid(), null, null));

        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public async Task UpdateUserAppearance_WithThemeOnly_HasNoErrors() {
        var result = await new UpdateUserAppearanceCommandValidator().TestValidateAsync(
            new UpdateUserAppearanceCommand(Guid.NewGuid(), "dark", null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ UpdateGoals â”€â”€

    [Fact]
    public async Task UpdateGoals_WithNullUserId_HasError() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateGoals_WithNegativeCalorieTarget_HasError() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(Guid.NewGuid(), -1, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.DailyCalorieTarget);
    }

    [Fact]
    public async Task UpdateGoals_WithValidTargets_NoErrors() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(Guid.NewGuid(), 2000, 150, 70, 250, 30, 2500, 75, 80, null, null, null, null, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateGoals_WithNegativeMondayCalories_HasError() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, -100, null, null, null, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.MondayCalories);
    }

    // â”€â”€ Query validators (UserId-only) â”€â”€

    [Fact]
    public async Task UpdateGoals_WithInfiniteProteinTarget_HasError() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(
            Guid.NewGuid(),
            DailyCalorieTarget: null,
            ProteinTarget: double.PositiveInfinity,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            WaterGoal: null,
            DesiredWeight: null,
            DesiredWaist: null));

        result.ShouldHaveValidationErrorFor(c => c.ProteinTarget);
    }

    [Fact]
    public async Task UpdateGoals_WithNaNMondayCalories_HasError() {
        var v = new UpdateGoalsCommandValidator();
        var result = await v.TestValidateAsync(new UpdateGoalsCommand(
            Guid.NewGuid(),
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            WaterGoal: null,
            DesiredWeight: null,
            DesiredWaist: null,
            MondayCalories: double.NaN));

        result.ShouldHaveValidationErrorFor(c => c.MondayCalories);
    }

    [Fact]
    public async Task GetDesiredWaist_WithNullUserId_HasError() {
        var result = await new GetDesiredWaistQueryValidator().TestValidateAsync(new GetDesiredWaistQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetDesiredWeight_WithNullUserId_HasError() {
        var result = await new GetDesiredWeightQueryValidator().TestValidateAsync(new GetDesiredWeightQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetUserById_WithNullUserId_HasError() {
        var result = await new GetUserByIdQueryValidator().TestValidateAsync(new GetUserByIdQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetUserGoals_WithNullUserId_HasError() {
        var result = await new GetUserGoalsQueryValidator().TestValidateAsync(new GetUserGoalsQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
