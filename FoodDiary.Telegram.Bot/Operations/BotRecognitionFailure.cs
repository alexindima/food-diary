namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotRecognitionFailure {
    internal static string Format(string code, bool russian) => code switch {
        "Ai.ConsentRequired" => russian
            ? "Для распознавания нужно согласие на обработку фото с помощью ИИ. Откройте дневник и ознакомьтесь с условиями. Можно также добавить еду вручную."
            : "Photo recognition requires your consent to AI processing. Open the diary to review the terms, or add food manually.",
        _ => russian
            ? "Месячный лимит распознавания исчерпан. Можно добавить еду вручную в дневнике или повторить после обновления лимита."
            : "Your monthly AI quota has been reached. Add food manually in the diary, or try again after your quota resets.",
    };
}
