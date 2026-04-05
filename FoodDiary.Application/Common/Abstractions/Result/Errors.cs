namespace FoodDiary.Application.Common.Abstractions.Result;

public static class Errors {
    public static class Product {
        public static Error NotFound(Guid id) => new(
            "Product.NotFound",
            $"Product with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error NotAccessible(Guid id) => new(
            "Product.NotAccessible",
            $"Product with ID {id} does not belong to the current user or was not found.",
            kind: ErrorKind.NotFound);

        public static Error AlreadyExists(string barcode) => new(
            "Product.AlreadyExists",
            $"Product with barcode {barcode} already exists.",
            kind: ErrorKind.Conflict);

        public static Error InvalidData(string message) => new(
            "Product.InvalidData",
            message,
            kind: ErrorKind.Internal);
    }

    public static class Recipe {
        public static Error NotFound(Guid id) => new(
            "Recipe.NotFound",
            $"Recipe with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error NotAccessible(Guid id) => new(
            "Recipe.NotAccessible",
            $"Recipe with ID {id} does not belong to the current user or was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidData(string message) => new(
            "Recipe.InvalidData",
            message,
            kind: ErrorKind.Internal);
    }

    public static class Consumption {
        public static Error NotFound(Guid id) => new(
            "Consumption.NotFound",
            $"Consumption with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidData(string message) => new(
            "Consumption.InvalidData",
            message,
            kind: ErrorKind.Internal);
    }

    public static class User {
        public static Error NotFound(Guid id) => new(
            "User.NotFound",
            $"User with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidPassword => new(
            "User.InvalidPassword",
            "The current password is incorrect.",
            kind: ErrorKind.Unauthorized);

        public static Error NotFound() => new(
            "User.NotFound",
            "User was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidCredentials => new(
            "User.InvalidCredentials",
            "Invalid email or password.",
            kind: ErrorKind.Unauthorized);

        public static Error EmailAlreadyExists => new(
            "User.EmailAlreadyExists",
            "A user with this email already exists.",
            kind: ErrorKind.Conflict);
    }

    public static class Authentication {
        public static Error InvalidCredentials => new(
            "Authentication.InvalidCredentials",
            "Invalid email or password.",
            kind: ErrorKind.Unauthorized);

        public static Error InvalidToken => new(
            "Authentication.InvalidToken",
            "Invalid authorization token.",
            kind: ErrorKind.Unauthorized);

        public static Error GoogleInvalidToken => new(
            "Authentication.GoogleInvalidToken",
            "Google credential is invalid.",
            kind: ErrorKind.Unauthorized);

        public static Error GoogleNotConfigured => new(
            "Authentication.GoogleNotConfigured",
            "Google authentication is not configured.",
            kind: ErrorKind.Internal);

        public static Error GoogleEmailNotVerified => new(
            "Authentication.GoogleEmailNotVerified",
            "Google account email is not verified.",
            kind: ErrorKind.Unauthorized);

        public static Error AccountDeleted => new(
            "Authentication.AccountDeleted",
            "Account is scheduled for deletion.",
            kind: ErrorKind.Unauthorized);

        public static Error AccountNotDeleted => new(
            "Authentication.AccountNotDeleted",
            "Account is already active.",
            kind: ErrorKind.Conflict);

        public static Error TelegramInvalidData => new(
            "Authentication.TelegramInvalidData",
            "Telegram auth data is invalid.",
            kind: ErrorKind.Validation);

        public static Error TelegramAuthExpired => new(
            "Authentication.TelegramAuthExpired",
            "Telegram auth data has expired.",
            kind: ErrorKind.Unauthorized);

        public static Error TelegramNotLinked => new(
            "Authentication.TelegramNotLinked",
            "Telegram account is not linked.",
            kind: ErrorKind.NotFound);

        public static Error TelegramAlreadyLinked => new(
            "Authentication.TelegramAlreadyLinked",
            "Telegram account is already linked to another user.",
            kind: ErrorKind.Conflict);

        public static Error TelegramNotConfigured => new(
            "Authentication.TelegramNotConfigured",
            "Telegram authentication is not configured.",
            kind: ErrorKind.Internal);

        public static Error TelegramBotNotConfigured => new(
            "Authentication.TelegramBotNotConfigured",
            "Telegram bot authentication is not configured.",
            kind: ErrorKind.Internal);

        public static Error TelegramBotInvalidSecret => new(
            "Authentication.TelegramBotInvalidSecret",
            "Telegram bot secret is invalid.",
            kind: ErrorKind.Unauthorized);

        public static Error AdminSsoInvalidCode => new(
            "Authentication.AdminSsoInvalidCode",
            "Admin SSO code is invalid or expired.",
            kind: ErrorKind.Unauthorized);

        public static Error AdminSsoForbidden => new(
            "Authentication.AdminSsoForbidden",
            "User is not allowed to access admin SSO.",
            kind: ErrorKind.Forbidden);
    }

    public static class Validation {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Field {field} is required.",
            CreateDetails(field, $"Field {field} is required."),
            kind: ErrorKind.Validation);

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Field {field} is invalid: {reason}",
            CreateDetails(field, $"Field {field} is invalid: {reason}"),
            kind: ErrorKind.Validation);

        private static IReadOnlyDictionary<string, string[]> CreateDetails(string field, string message) =>
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                [field] = [message],
            };
    }

    public static class WeightEntry {
        public static Error NotFound(Guid id) => new(
            "WeightEntry.NotFound",
            $"Weight entry with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WeightEntry.AlreadyExists",
            $"Weight entry for {date:yyyy-MM-dd} already exists.",
            kind: ErrorKind.Conflict);
    }

    public static class WaistEntry {
        public static Error NotFound(Guid id) => new(
            "WaistEntry.NotFound",
            $"Waist entry with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error AlreadyExists(DateTime date) => new(
            "WaistEntry.AlreadyExists",
            $"Waist entry for {date:yyyy-MM-dd} already exists.",
            kind: ErrorKind.Conflict);
    }

    public static class HydrationEntry {
        public static Error NotFound(Guid id) => new(
            "HydrationEntry.NotFound",
            $"Hydration entry with id '{id}' not found",
            kind: ErrorKind.NotFound);
    }

    public static class DailyAdvice {
        public static Error NotFound(string? locale = null) => new(
            "DailyAdvice.NotFound",
            locale is null
                ? "Daily advice items are not configured."
                : $"Daily advice items are not configured for locale '{locale}'.",
            kind: ErrorKind.NotFound);
    }

    public static class ShoppingList {
        public static Error NotFound(Guid id) => new(
            "ShoppingList.NotFound",
            $"Shopping list with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error CurrentNotFound() => new(
            "ShoppingList.NotFound",
            "Shopping list was not found.",
            kind: ErrorKind.NotFound);
    }

    public static class Cycle {
        public static Error NotFound(Guid id) => new(
            "Cycle.NotFound",
            $"Cycle with ID {id} was not found.",
            kind: ErrorKind.NotFound);
    }

    public static class CycleDay {
        public static Error NotFound(DateTime date) => new(
            "CycleDay.NotFound",
            $"Cycle day for {date:yyyy-MM-dd} was not found.",
            kind: ErrorKind.NotFound);
    }

    public static class Ai {
        public static Error ImageNotFound(Guid id) => new(
            "Ai.ImageNotFound",
            $"Image asset with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error Forbidden() => new(
            "Ai.Forbidden",
            "Image asset does not belong to the current user.",
            kind: ErrorKind.Forbidden);

        public static Error EmptyItems() => new(
            "Ai.EmptyItems",
            "No food items were provided.",
            kind: ErrorKind.Validation);

        public static Error OpenAiFailed(string reason) => new(
            "Ai.OpenAiFailed",
            reason,
            kind: ErrorKind.ExternalFailure);

        public static Error InvalidResponse(string reason) => new(
            "Ai.InvalidResponse",
            reason,
            kind: ErrorKind.ExternalFailure);

        public static Error QuotaExceeded() => new(
            "Ai.QuotaExceeded",
            "AI token quota exceeded for the current month.",
            kind: ErrorKind.RateLimited);
    }

    public static class Image {
        public static Error InvalidData(string message) => new(
            "Image.InvalidData",
            message,
            kind: ErrorKind.Validation);

        public static Error NotFound(Guid id) => new(
            "Image.NotFound",
            $"Image asset with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error Forbidden() => new(
            "Image.Forbidden",
            "Image asset does not belong to the current user.",
            kind: ErrorKind.Forbidden);

        public static Error InUse() => new(
            "Image.InUse",
            "Image asset is already in use.",
            kind: ErrorKind.Conflict);

        public static Error StorageError() => new(
            "Image.StorageError",
            "Failed to remove image from storage.",
            kind: ErrorKind.ExternalFailure);
    }

    public static class Dietologist {
        public static Error InvitationNotFound => new(
            "Dietologist.InvitationNotFound",
            "Dietologist invitation was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvitationExpired => new(
            "Dietologist.InvitationExpired",
            "Dietologist invitation has expired.",
            kind: ErrorKind.Validation);

        public static Error InvitationInvalidToken => new(
            "Dietologist.InvitationInvalidToken",
            "Invitation token is invalid.",
            kind: ErrorKind.Unauthorized);

        public static Error AlreadyHasDietologist => new(
            "Dietologist.AlreadyHasDietologist",
            "You already have an active dietologist.",
            kind: ErrorKind.Conflict);

        public static Error PendingInvitationExists => new(
            "Dietologist.PendingInvitationExists",
            "A pending invitation already exists.",
            kind: ErrorKind.Conflict);

        public static Error CannotInviteSelf => new(
            "Dietologist.CannotInviteSelf",
            "You cannot invite yourself as a dietologist.",
            kind: ErrorKind.Validation);

        public static Error AccessDenied => new(
            "Dietologist.AccessDenied",
            "You do not have access to this client's data.",
            kind: ErrorKind.Forbidden);

        public static Error PermissionDenied => new(
            "Dietologist.PermissionDenied",
            "The client has not shared this data category.",
            kind: ErrorKind.Forbidden);

        public static Error NoActiveRelationship => new(
            "Dietologist.NoActiveRelationship",
            "No active dietologist relationship found.",
            kind: ErrorKind.NotFound);
    }

    public static class Fasting {
        public static Error AlreadyActive => new(
            "Fasting.AlreadyActive",
            "A fasting session is already active.",
            kind: ErrorKind.Conflict);

        public static Error NoActiveSession => new(
            "Fasting.NoActiveSession",
            "No active fasting session found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidProtocol => new(
            "Fasting.InvalidProtocol",
            "Invalid fasting protocol.",
            kind: ErrorKind.Validation);
    }

    public static class FavoriteMeal {
        public static Error NotFound(Guid id) => new(
            "FavoriteMeal.NotFound",
            $"Favorite meal with id '{id}' was not found.",
            kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteMeal.AlreadyExists",
            "This meal is already in favorites.",
            kind: ErrorKind.Conflict);
    }

    public static class MealPlan {
        public static Error NotFound(Guid id) => new(
            "MealPlan.NotFound",
            $"Meal plan with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidId => new(
            "MealPlan.InvalidId",
            "Meal plan ID is required.",
            kind: ErrorKind.Validation);

        public static Error NotCurated => new(
            "MealPlan.NotCurated",
            "Only curated meal plans can be adopted.",
            kind: ErrorKind.Validation);
    }

    public static class Exercise {
        public static Error NotFound(Guid id) => new(
            "Exercise.NotFound",
            $"Exercise entry with ID {id} was not found.",
            kind: ErrorKind.NotFound);

        public static Error InvalidDuration => new(
            "Exercise.InvalidDuration",
            "Exercise duration must be positive.",
            kind: ErrorKind.Validation);

        public static Error InvalidCalories => new(
            "Exercise.InvalidCalories",
            "Calories burned must be non-negative.",
            kind: ErrorKind.Validation);
    }

    public static class Lesson {
        public static Error NotFound(Guid id) => new(
            "Lesson.NotFound",
            $"Lesson with ID {id} was not found.",
            kind: ErrorKind.NotFound);
    }

    public static class Usda {
        public static Error FoodNotFound(int fdcId) => new(
            "Usda.FoodNotFound",
            $"USDA food with FDC ID {fdcId} was not found.",
            kind: ErrorKind.NotFound);
    }
}
