using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Antiforgery;
using OpenIddict.Abstractions;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.RecipePlugin;

internal static class RecipePluginConsentPage {
    private static readonly string[] AuthorizationParameters = [
        "client_id", "redirect_uri", "response_type", "response_mode", "scope", "state",
        "code_challenge", "code_challenge_method",
    ];

    public static string Render(OpenIddictRequest request, AntiforgeryTokenSet tokens, string decisionField) {
        bool russian = IsRussian();
        StringBuilder page = Open(russian);
        page.Append(russian ? "<p>Разрешить ChatGPT следующие действия в твоём аккаунте?</p>"
            : "<p>Allow ChatGPT to take these actions in your account?</p>");
        page.Append("<ul>");
        if (request.HasScope(RecipePluginAccess.RecipeScope)) {
            page.Append(russian ? "<li>Сохранять личные рецепты</li>" : "<li>Save private recipes</li>");
        }
        if (request.HasScope(RecipePluginAccess.ShoppingListScope)) {
            page.Append(russian ? "<li>Создавать списки покупок</li>" : "<li>Create shopping lists</li>");
        }
        page.Append("</ul><form method=\"post\" action=\"").Append(RecipePluginAccess.AuthorizationPath).Append("\">");
        foreach (string name in AuthorizationParameters) {
            if ((string?)request.GetParameter(name) is { } value) {
                AppendHidden(page, name, value);
            }
        }
        AppendHidden(page, "resource", request.GetResources().Single());
        AppendHidden(page, tokens.FormFieldName, tokens.RequestToken ??
            throw new InvalidOperationException("A consent form requires an antiforgery token."));
        AppendButton(page, decisionField, "approve", russian ? "Разрешить" : "Allow");
        AppendButton(page, decisionField, "deny", russian ? "Отмена" : "Cancel");
        return page.Append("</form></main></body></html>").ToString();
    }

    public static string SignIn(Uri webOrigin) {
        bool russian = IsRussian();
        Uri signIn = new(webOrigin, "/?auth=login");
        StringBuilder page = Open(russian);
        page.Append("<p><a target=\"_blank\" rel=\"noopener noreferrer\" href=\"")
            .Append(HtmlEncoder.Default.Encode(signIn.AbsoluteUri)).Append("\">")
            .Append(russian ? "Войти в FoodDiary" : "Sign in to FoodDiary")
            .Append("</a></p>")
            .Append(russian ? "<p>После входа обнови эту страницу.</p>" : "<p>After signing in, reload this page.</p>");
        return page.Append("</main></body></html>").ToString();
    }

    public static string Continue(Uri destination) {
        bool russian = IsRussian();
        string link = HtmlEncoder.Default.Encode(destination.AbsoluteUri);
        return Open(russian, link)
            .Append("<p><a rel=\"noreferrer\" href=\"").Append(link).Append("\">")
            .Append(russian ? "Вернуться в ChatGPT" : "Return to ChatGPT")
            .Append("</a></p></main></body></html>").ToString();
    }

    private static bool IsRussian() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "ru";

    private static StringBuilder Open(bool russian, string? redirectLink = null) {
        string title = russian ? "Подключить ChatGPT к FoodDiary" : "Connect ChatGPT to FoodDiary";
        StringBuilder page = new StringBuilder("<!doctype html><html lang=\"")
            .Append(russian ? "ru" : "en")
            .Append("\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>")
            .Append(title).Append("</title>");
        if (redirectLink is not null) {
            page.Append("<meta http-equiv=\"refresh\" content=\"0;url=").Append(redirectLink).Append("\">");
        }
        return page.Append("</head><body><main><h1>").Append(title).Append("</h1>");
    }

    private static void AppendHidden(StringBuilder page, string name, string value) =>
        page.Append("<input type=\"hidden\" name=\"").Append(HtmlEncoder.Default.Encode(name))
            .Append("\" value=\"").Append(HtmlEncoder.Default.Encode(value)).Append("\">");

    private static void AppendButton(StringBuilder page, string name, string value, string label) =>
        page.Append("<button type=\"submit\" name=\"").Append(HtmlEncoder.Default.Encode(name))
            .Append("\" value=\"").Append(HtmlEncoder.Default.Encode(value)).Append("\">")
            .Append(label).Append("</button>");
}
