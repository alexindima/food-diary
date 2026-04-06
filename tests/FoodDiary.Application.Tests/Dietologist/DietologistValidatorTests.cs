using FluentValidation.TestHelper;
using FoodDiary.Application.Dietologist.Commands.AcceptInvitation;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Tests.Dietologist;

public class DietologistValidatorTests {
    [Fact]
    public async Task InviteDietologist_WithNullUserId_HasError() {
        var validator = new InviteDietologistCommandValidator();
        var command = new InviteDietologistCommand(
            null, "diet@example.com", new DietologistPermissionsInput(true, true, true, true, true, true));
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task InviteDietologist_WithEmptyEmail_HasError() {
        var validator = new InviteDietologistCommandValidator();
        var command = new InviteDietologistCommand(
            Guid.NewGuid(), "", new DietologistPermissionsInput(true, true, true, true, true, true));
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DietologistEmail);
    }

    [Fact]
    public async Task InviteDietologist_WithInvalidEmail_HasError() {
        var validator = new InviteDietologistCommandValidator();
        var command = new InviteDietologistCommand(
            Guid.NewGuid(), "not-an-email", new DietologistPermissionsInput(true, true, true, true, true, true));
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DietologistEmail);
    }

    [Fact]
    public async Task InviteDietologist_WithNullPermissions_HasError() {
        var validator = new InviteDietologistCommandValidator();
        var command = new InviteDietologistCommand(Guid.NewGuid(), "diet@example.com", null!);
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Permissions);
    }

    [Fact]
    public async Task InviteDietologist_WithValidCommand_NoErrors() {
        var validator = new InviteDietologistCommandValidator();
        var command = new InviteDietologistCommand(
            Guid.NewGuid(), "diet@example.com", new DietologistPermissionsInput(true, true, true, true, true, true));
        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task AcceptInvitation_WithEmptyInvitationId_HasError() {
        var validator = new AcceptInvitationCommandValidator();
        var command = new AcceptInvitationCommand(Guid.Empty, "token", Guid.NewGuid());
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.InvitationId);
    }

    [Fact]
    public async Task AcceptInvitation_WithEmptyToken_HasError() {
        var validator = new AcceptInvitationCommandValidator();
        var command = new AcceptInvitationCommand(Guid.NewGuid(), "", Guid.NewGuid());
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Token);
    }

    [Fact]
    public async Task AcceptInvitation_WithValidCommand_NoErrors() {
        var validator = new AcceptInvitationCommandValidator();
        var command = new AcceptInvitationCommand(Guid.NewGuid(), "token-value", Guid.NewGuid());
        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateRecommendation_WithEmptyText_HasError() {
        var validator = new CreateRecommendationCommandValidator();
        var command = new CreateRecommendationCommand(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task CreateRecommendation_WithTooLongText_HasError() {
        var validator = new CreateRecommendationCommandValidator();
        var command = new CreateRecommendationCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 2001));
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public async Task CreateRecommendation_WithEmptyClientUserId_HasError() {
        var validator = new CreateRecommendationCommandValidator();
        var command = new CreateRecommendationCommand(Guid.NewGuid(), Guid.Empty, "Eat more veggies");
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ClientUserId);
    }

    [Fact]
    public async Task CreateRecommendation_WithValidCommand_NoErrors() {
        var validator = new CreateRecommendationCommandValidator();
        var command = new CreateRecommendationCommand(Guid.NewGuid(), Guid.NewGuid(), "Eat more veggies");
        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
