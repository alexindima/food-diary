namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotRecognitionFailure {
    internal const string PremiumRequired = "Telegram.PhotoPremiumRequired";

    internal static string Format(string code, bool russian) => code switch {
        PremiumRequired => russian
            ? "Автоматическое распознавание еды по фото доступно с подпиской Food Diary Premium. Приём пищи можно добавить вручную."
            : "Automatic food recognition from photos is available with Food Diary Premium. You can add the meal manually.",
        "Ai.ConsentRequired" => russian
            ? "Для распознавания нужно согласие на обработку фото с помощью ИИ. Откройте дневник и ознакомьтесь с условиями. Можно также добавить еду вручную."
            : "Photo recognition requires your consent to AI processing. Open the diary to review the terms, or add food manually.",
        _ => russian
            ? "Месячный лимит распознавания исчерпан. Можно добавить еду вручную в дневнике или повторить после обновления лимита."
            : "Your monthly AI quota has been reached. Add food manually in the diary, or try again after your quota resets.",
    };
}
