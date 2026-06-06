namespace FoodDiary.Application.Abstractions.Common.Abstractions.Result;

public static class Errors {
    public static class Product {
        public static Error NotFound(Guid id) => new(
            "Product.NotFound",
            $"Product with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error NotAccessible(Guid id) => new(
            "Product.NotAccessible",
            $"Product with ID {id} does not belong to the current user or was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(string barcode) => new(
            "Product.AlreadyExists",
            $"Product with barcode {barcode} already exists.",
            Kind: ErrorKind.Conflict);

        public static Error InvalidData(string message) => new(
            "Product.InvalidData",
            message,
            Kind: ErrorKind.Internal);
    }

    public static class Recipe {
        public static Error NotFound(Guid id) => new(
            "Recipe.NotFound",
            $"Recipe with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error NotAccessible(Guid id) => new(
            "Recipe.NotAccessible",
            $"Recipe with ID {id} does not belong to the current user or was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidData(string message) => new(
            "Recipe.InvalidData",
            message,
            Kind: ErrorKind.Internal);
    }

    public static class Consumption {
        public static Error NotFound(Guid id) => new(
            "Consumption.NotFound",
            $"Consumption with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidData(string message) => new(
            "Consumption.InvalidData",
            message,
            Kind: ErrorKind.Internal);
    }

    public static class User {
        public static Error NotFound(Guid id) => new(
            "User.NotFound",
            $"User with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidPassword => new(
            "User.InvalidPassword",
            "The current password is incorrect.",
            Kind: ErrorKind.Unauthorized);

        public static Error PasswordNotSet => new(
            "User.PasswordNotSet",
            "Password is not configured for this account.",
            Kind: ErrorKind.Conflict);

        public static Error PasswordAlreadySet => new(
            "User.PasswordAlreadySet",
            "Password is already configured for this account.",
            Kind: ErrorKind.Conflict);

        public static Error NotFound() => new(
            "User.NotFound",
            "User was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidCredentials => new(
            "User.InvalidCredentials",
            "Invalid email or password.",
            Kind: ErrorKind.Unauthorized);

        public static Error EmailAlreadyExists => new(
            "User.EmailAlreadyExists",
            "A user with this email already exists.",
            Kind: ErrorKind.Conflict);
    }

    public static class Authentication {
        public static Error InvalidCredentials => new(
            "Authentication.InvalidCredentials",
            "Invalid email or password.",
            Kind: ErrorKind.Unauthorized);

        public static Error InvalidToken => new(
            "Authentication.InvalidToken",
            "Invalid authorization token.",
            Kind: ErrorKind.Unauthorized);

        public static Error GoogleInvalidToken => new(
            "Authentication.GoogleInvalidToken",
            "Google credential is invalid.",
            Kind: ErrorKind.Unauthorized);

        public static Error GoogleNotConfigured => new(
            "Authentication.GoogleNotConfigured",
            "Google authentication is not configured.",
            Kind: ErrorKind.Internal);

        public static Error GoogleEmailNotVerified => new(
            "Authentication.GoogleEmailNotVerified",
            "Google account email is not verified.",
            Kind: ErrorKind.Unauthorized);

        public static Error AccountDeleted => new(
            "Authentication.AccountDeleted",
            "Account is scheduled for deletion.",
            Kind: ErrorKind.Unauthorized);

        public static Error AccountNotDeleted => new(
            "Authentication.AccountNotDeleted",
            "Account is already active.",
            Kind: ErrorKind.Conflict);

        public static Error TelegramInvalidData => new(
            "Authentication.TelegramInvalidData",
            "Telegram auth data is invalid.",
            Kind: ErrorKind.Validation);

        public static Error TelegramAuthExpired => new(
            "Authentication.TelegramAuthExpired",
            "Telegram auth data has expired.",
            Kind: ErrorKind.Unauthorized);

        public static Error TelegramNotLinked => new(
            "Authentication.TelegramNotLinked",
            "Telegram account is not linked.",
            Kind: ErrorKind.NotFound);

        public static Error TelegramAlreadyLinked => new(
            "Authentication.TelegramAlreadyLinked",
            "Telegram account is already linked to another user.",
            Kind: ErrorKind.Conflict);

        public static Error TelegramNotConfigured => new(
            "Authentication.TelegramNotConfigured",
            "Telegram authentication is not configured.",
            Kind: ErrorKind.Internal);

        public static Error TelegramBotNotConfigured => new(
            "Authentication.TelegramBotNotConfigured",
            "Telegram bot authentication is not configured.",
            Kind: ErrorKind.Internal);

        public static Error TelegramBotInvalidSecret => new(
            "Authentication.TelegramBotInvalidSecret",
            "Telegram bot secret is invalid.",
            Kind: ErrorKind.Unauthorized);

        public static Error AdminSsoInvalidCode => new(
            "Authentication.AdminSsoInvalidCode",
            "Admin SSO code is invalid or expired.",
            Kind: ErrorKind.Unauthorized);

        public static Error AdminSsoForbidden => new(
            "Authentication.AdminSsoForbidden",
            "User is not allowed to access admin SSO.",
            Kind: ErrorKind.Forbidden);

        public static Error ImpersonationForbidden => new(
            "Authentication.ImpersonationForbidden",
            "User cannot be impersonated.",
            Kind: ErrorKind.Forbidden);

        public static Error ImpersonationActionForbidden => new(
            "Authentication.ImpersonationActionForbidden",
            "This action is not allowed while impersonating a user.",
            Kind: ErrorKind.Forbidden);
    }

    public static class Validation {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Field {field} is required.",
            CreateDetails(field, $"Field {field} is required."),
            Kind: ErrorKind.Validation);

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Field {field} is invalid: {reason}",
            CreateDetails(field, $"Field {field} is invalid: {reason}"),
            Kind: ErrorKind.Validation);

        private static IReadOnlyDictionary<string, string[]> CreateDetails(string field, string message) =>
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                [field] = [message],
            };
    }

    public static class WeightEntry {
        public static Error NotFound(Guid id) => new(
            "WeightEntry.NotFound",
            $"Weight entry with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WeightEntry.AlreadyExists",
            $"Weight entry for {date:yyyy-MM-dd} already exists.",
            Kind: ErrorKind.Conflict);
    }

    public static class WaistEntry {
        public static Error NotFound(Guid id) => new(
            "WaistEntry.NotFound",
            $"Waist entry with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WaistEntry.AlreadyExists",
            $"Waist entry for {date:yyyy-MM-dd} already exists.",
            Kind: ErrorKind.Conflict);
    }

    public static class HydrationEntry {
        public static Error NotFound(Guid id) => new(
            "HydrationEntry.NotFound",
            $"Hydration entry with id '{id}' not found",
            Kind: ErrorKind.NotFound);
    }

    public static class DailyAdvice {
        public static Error NotFound(string? locale = null) => new(
            "DailyAdvice.NotFound",
            locale is null
                ? "Daily advice items are not configured."
                : $"Daily advice items are not configured for locale '{locale}'.",
            Kind: ErrorKind.NotFound);
    }

    public static class ShoppingList {
        public static Error NotFound(Guid id) => new(
            "ShoppingList.NotFound",
            $"Shopping list with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error CurrentNotFound() => new(
            "ShoppingList.NotFound",
            "Shopping list was not found.",
            Kind: ErrorKind.NotFound);
    }

    public static class Cycle {
        public static Error NotFound(Guid id) => new(
            "Cycle.NotFound",
            $"Cycle with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }

    public static class CycleDay {
        public static Error NotFound(DateTime date) => new(
            "CycleDay.NotFound",
            $"Cycle day for {date:yyyy-MM-dd} was not found.",
            Kind: ErrorKind.NotFound);
    }

    public static class Ai {
        public static Error ImageNotFound(Guid id) => new(
            "Ai.ImageNotFound",
            $"Image asset with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error Forbidden() => new(
            "Ai.Forbidden",
            "Image asset does not belong to the current user.",
            Kind: ErrorKind.Forbidden);

        public static Error EmptyItems() => new(
            "Ai.EmptyItems",
            "No food items were provided.",
            Kind: ErrorKind.Validation);

        public static Error OpenAiFailed(string reason) => new(
            "Ai.OpenAiFailed",
            reason,
            Kind: ErrorKind.ExternalFailure);

        public static Error InvalidResponse(string reason) => new(
            "Ai.InvalidResponse",
            reason,
            Kind: ErrorKind.ExternalFailure);

        public static Error QuotaExceeded() => new(
            "Ai.QuotaExceeded",
            "AI token quota exceeded for the current month.",
            Kind: ErrorKind.RateLimited);
    }

    public static class Billing {
        public static Error InvalidPlan => new(
            "Billing.InvalidPlan",
            "Billing plan is invalid.",
            Kind: ErrorKind.Validation);

        public static Error InvalidProvider(string provider) => new(
            "Billing.InvalidProvider",
            $"'{provider}' is not a valid billing provider.",
            Kind: ErrorKind.Validation);

        public static Error ProviderNotConfigured(string provider) => new(
            "Billing.ProviderNotConfigured",
            $"Billing provider '{provider}' is not configured.",
            Kind: ErrorKind.ExternalFailure);

        public static Error ProviderOperationFailed(string provider, string reason) => new(
            "Billing.ProviderOperationFailed",
            $"Billing provider '{provider}' request failed: {reason}",
            Kind: ErrorKind.ExternalFailure);

        public static Error SubscriptionAlreadyActive => new(
            "Billing.SubscriptionAlreadyActive",
            "Premium subscription is already active for the current user.",
            Kind: ErrorKind.Conflict);

        public static Error TrialAlreadyUsed => new(
            "Billing.TrialAlreadyUsed",
            "Premium trial has already been used for the current user.",
            Kind: ErrorKind.Conflict);

        public static Error CustomerPortalUnavailable => new(
            "Billing.CustomerPortalUnavailable",
            "Billing management is not available for the current user.",
            Kind: ErrorKind.NotFound);

        public static Error WebhookValidationFailed(string reason) => new(
            "Billing.WebhookValidationFailed",
            reason,
            Kind: ErrorKind.Validation);
    }

    public static class Image {
        public static Error InvalidData(string message) => new(
            "Image.InvalidData",
            message,
            Kind: ErrorKind.Validation);

        public static Error NotFound(Guid id) => new(
            "Image.NotFound",
            $"Image asset with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error Forbidden() => new(
            "Image.Forbidden",
            "Image asset does not belong to the current user.",
            Kind: ErrorKind.Forbidden);

        public static Error InUse() => new(
            "Image.InUse",
            "Image asset is already in use.",
            Kind: ErrorKind.Conflict);

        public static Error StorageError() => new(
            "Image.StorageError",
            "Failed to remove image from storage.",
            Kind: ErrorKind.ExternalFailure);
    }

    public static class Dietologist {
        public static Error InvitationNotFound => new(
            "Dietologist.InvitationNotFound",
            "Dietologist invitation was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvitationExpired => new(
            "Dietologist.InvitationExpired",
            "Dietologist invitation has expired.",
            Kind: ErrorKind.Validation);

        public static Error InvitationInvalidToken => new(
            "Dietologist.InvitationInvalidToken",
            "Invitation token is invalid.",
            Kind: ErrorKind.Unauthorized);

        public static Error AlreadyHasDietologist => new(
            "Dietologist.AlreadyHasDietologist",
            "You already have an active dietologist.",
            Kind: ErrorKind.Conflict);

        public static Error PendingInvitationExists => new(
            "Dietologist.PendingInvitationExists",
            "A pending invitation already exists.",
            Kind: ErrorKind.Conflict);

        public static Error CannotInviteSelf => new(
            "Dietologist.CannotInviteSelf",
            "You cannot invite yourself as a dietologist.",
            Kind: ErrorKind.Validation);

        public static Error AccessDenied => new(
            "Dietologist.AccessDenied",
            "You do not have access to this client's data.",
            Kind: ErrorKind.Forbidden);

        public static Error PermissionDenied => new(
            "Dietologist.PermissionDenied",
            "The client has not shared this data category.",
            Kind: ErrorKind.Forbidden);

        public static Error NoActiveRelationship => new(
            "Dietologist.NoActiveRelationship",
            "No active dietologist relationship found.",
            Kind: ErrorKind.NotFound);
    }

    public static class Fasting {
        public static Error AlreadyActive => new(
            "Fasting.AlreadyActive",
            "A fasting session is already active.",
            Kind: ErrorKind.Conflict);

        public static Error NoActiveSession => new(
            "Fasting.NoActiveSession",
            "No active fasting session found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidProtocol => new(
            "Fasting.InvalidProtocol",
            "Invalid fasting protocol.",
            Kind: ErrorKind.Validation);

        public static Error InvalidCyclicAction(string message) => new(
            "Fasting.InvalidCyclicAction",
            message,
            Kind: ErrorKind.Validation);
    }

    public static class FavoriteMeal {
        public static Error NotFound(Guid id) => new(
            "FavoriteMeal.NotFound",
            $"Favorite meal with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteMeal.AlreadyExists",
            "This meal is already in favorites.",
            Kind: ErrorKind.Conflict);
    }

    public static class FavoriteProduct {
        public static Error NotFound(Guid id) => new(
            "FavoriteProduct.NotFound",
            $"Favorite product with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteProduct.AlreadyExists",
            "This product is already in favorites.",
            Kind: ErrorKind.Conflict);
    }

    public static class FavoriteRecipe {
        public static Error NotFound(Guid id) => new(
            "FavoriteRecipe.NotFound",
            $"Favorite recipe with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteRecipe.AlreadyExists",
            "This recipe is already in favorites.",
            Kind: ErrorKind.Conflict);
    }

    public static class MealPlan {
        public static Error NotFound(Guid id) => new(
            "MealPlan.NotFound",
            $"Meal plan with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidId => new(
            "MealPlan.InvalidId",
            "Meal plan ID is required.",
            Kind: ErrorKind.Validation);

        public static Error NotCurated => new(
            "MealPlan.NotCurated",
            "Only curated meal plans can be adopted.",
            Kind: ErrorKind.Validation);
    }

    public static class Exercise {
        public static Error NotFound(Guid id) => new(
            "Exercise.NotFound",
            $"Exercise entry with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error InvalidDuration => new(
            "Exercise.InvalidDuration",
            "Exercise duration must be positive.",
            Kind: ErrorKind.Validation);

        public static Error InvalidCalories => new(
            "Exercise.InvalidCalories",
            "Calories burned must be non-negative.",
            Kind: ErrorKind.Validation);
    }

    public static class Lesson {
        public static Error NotFound(Guid id) => new(
            "Lesson.NotFound",
            $"Lesson with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }

    public static class RecipeComment {
        public static Error NotFound(Guid id) => new(
            "RecipeComment.NotFound",
            $"Comment with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error NotAuthor => new(
            "RecipeComment.NotAuthor",
            "You are not the author of this comment.",
            Kind: ErrorKind.Forbidden);
    }

    public static class ContentReport {
        public static Error NotFound(Guid id) => new(
            "ContentReport.NotFound",
            $"Content report with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyReported => new(
            "ContentReport.AlreadyReported",
            "You have already reported this content.",
            Kind: ErrorKind.Conflict);
    }

    public static class Usda {
        public static Error FoodNotFound(int fdcId) => new(
            "Usda.FoodNotFound",
            $"USDA food with FDC ID {fdcId} was not found.",
            Kind: ErrorKind.NotFound);
    }

    public static class Wearable {
        public static Error InvalidProvider(string provider) => new(
            "Wearable.InvalidProvider",
            $"'{provider}' is not a valid wearable provider.",
            Kind: ErrorKind.Validation);

        public static Error ProviderNotConfigured(string provider) => new(
            "Wearable.ProviderNotConfigured",
            $"Wearable provider '{provider}' is not configured.",
            Kind: ErrorKind.Internal);

        public static Error NotConnected(string provider) => new(
            "Wearable.NotConnected",
            $"No active connection found for provider '{provider}'.",
            Kind: ErrorKind.NotFound);

        public static Error AuthFailed(string provider) => new(
            "Wearable.AuthFailed",
            $"Authentication with '{provider}' failed.",
            Kind: ErrorKind.Unauthorized);

        public static Error InvalidState => new(
            "Wearable.InvalidState",
            "Wearable authentication state is invalid or expired.",
            Kind: ErrorKind.Unauthorized);
    }

    public static class MailInbox {
        public static Error MessageNotFound(Guid id) => new(
            "MailInbox.MessageNotFound",
            $"Mail inbox message with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
