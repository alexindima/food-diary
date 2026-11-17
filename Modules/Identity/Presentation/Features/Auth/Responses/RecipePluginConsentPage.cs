using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Antiforgery;
using OpenIddict.Abstractions;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

internal static class RecipePluginConsentPage {
    // Use the FoodDiary auth shell's blue palette, neutral surface and touch-sized controls.
    private static readonly string Styles = """
        :root {
            color-scheme: light;
            --blue-50: #eff6ff;
            --blue-600: #2563eb;
            --blue-700: #1d4ed8;
            --text: #0f172a;
            --muted: #475569;
            --border: #dbe3ef;
            font: 1rem/1.5 system-ui, sans-serif;
            color: var(--text);
            background: #f1f5f9;
        }
        *, *::before, *::after { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            min-height: 100dvh;
            display: grid;
            place-items: center;
            padding: max(1.5rem, env(safe-area-inset-top)) 1rem max(1.5rem, env(safe-area-inset-bottom));
            background: linear-gradient(180deg, #e6efff, #f1f5f9 65%);
        }
        .consent-card {
            width: min(100%, 30rem);
            padding: clamp(1.5rem, 5vw, 2.25rem);
            border: 1px solid var(--border);
            border-radius: 1rem;
            background: #fff;
            box-shadow: 0 8px 24px rgb(15 23 42 / 8%);
        }
        .brand {
            display: flex;
            align-items: center;
            gap: .625rem;
            margin-bottom: 1.25rem;
            font-weight: 700;
        }
        .brand img { display: block; }
        h1 {
            margin: 0;
            max-width: 17ch;
            font-size: clamp(1.625rem, 5vw, 2rem);
            line-height: 1.16;
            letter-spacing: -.025em;
        }
        .lead, .hint { margin: .75rem 0 0; color: var(--muted); }
        .permissions {
            margin: 1.25rem 0 1.5rem;
            padding: 0;
            border: 1px solid var(--border);
            border-radius: .75rem;
            background: #f8fafc;
            list-style: none;
        }
        .permissions li {
            display: flex;
            align-items: center;
            gap: .75rem;
            min-height: 3rem;
            padding: .625rem .875rem;
            font-size: .9375rem;
            font-weight: 600;
        }
        .permissions li + li { border-top: 1px solid var(--border); }
        .permission-mark {
            display: grid;
            flex: 0 0 1.5rem;
            height: 1.5rem;
            place-items: center;
            border-radius: 50%;
            background: var(--blue-50);
        }
        .permission-mark::before {
            content: "";
            width: .62rem;
            height: .36rem;
            border: solid var(--blue-700);
            border-width: 0 0 2px 2px;
            transform: translateY(-1px) rotate(-45deg);
        }
        .actions { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: .75rem; }
        .button {
            display: inline-grid;
            min-height: 3rem;
            place-items: center;
            padding: .625rem 1.125rem;
            border: 1px solid var(--border);
            border-radius: .75rem;
            background: #fff;
            color: var(--text);
            font: inherit;
            font-weight: 650;
            text-align: center;
            text-decoration: none;
            cursor: pointer;
        }
        .button:hover { background: #f1f5f9; }
        .button.primary { border-color: var(--blue-600); background: var(--blue-600); color: #fff; }
        .button.primary:hover { border-color: var(--blue-700); background: var(--blue-700); }
        .button:focus-visible { outline: 3px solid var(--blue-700); outline-offset: 3px; }
        .standalone-action { margin-top: 1.25rem; width: 100%; }
        @media (max-width: 28rem) {
            .actions { grid-template-columns: 1fr; gap: .5rem; }
        }
        """.ReplaceLineEndings("\n");

    private static readonly string[] AuthorizationParameters = [
        "client_id", "redirect_uri", "response_type", "response_mode", "scope", "state",
        "code_challenge", "code_challenge_method",
    ];

    public static string StyleSource { get; } = $"'sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Styles)))}'";

    public static string Render(OpenIddictRequest request, AntiforgeryTokenSet tokens, string decisionField) {
        bool russian = IsRussian();
        StringBuilder page = Open(russian);
        page.Append("<p class=\"lead\">")
            .Append(russian ? "Разрешить ChatGPT следующие действия в твоём аккаунте?"
                : "Allow ChatGPT to take these actions in your account?")
            .Append("</p><ul class=\"permissions\" role=\"list\">");
        if (request.HasScope(RecipePluginAccess.RecipeScope)) {
            AppendPermission(page, russian ? "Сохранять личные рецепты" : "Save private recipes");
        }
        if (request.HasScope(RecipePluginAccess.ShoppingListScope)) {
            AppendPermission(page, russian ? "Создавать списки покупок" : "Create shopping lists");
        }
        page.Append("</ul><form class=\"actions\" method=\"post\" action=\"").Append(RecipePluginAccess.AuthorizationPath).Append("\">");
        foreach (string name in AuthorizationParameters) {
            if ((string?)request.GetParameter(name) is { } value) {
                AppendHidden(page, name, value);
            }
        }
        AppendHidden(page, "resource", request.GetResources().Single());
        AppendHidden(page, tokens.FormFieldName, tokens.RequestToken ??
            throw new InvalidOperationException("A consent form requires an antiforgery token."));
        AppendButton(page, "button primary", decisionField, "approve", russian ? "Разрешить" : "Allow");
        AppendButton(page, "button", decisionField, "deny", russian ? "Отмена" : "Cancel");
        return page.Append("</form></main></body></html>").ToString();
    }

    public static string SignIn(Uri webOrigin) {
        bool russian = IsRussian();
        Uri signIn = new(webOrigin, "/?auth=login");
        StringBuilder page = Open(russian);
        page.Append("<a class=\"button primary standalone-action\" target=\"_blank\" rel=\"noopener noreferrer\" href=\"")
            .Append(HtmlEncoder.Default.Encode(signIn.AbsoluteUri)).Append("\">")
            .Append(russian ? "Войти в FoodDiary" : "Sign in to FoodDiary")
            .Append("</a><p class=\"hint\">")
            .Append(russian ? "После входа обнови эту страницу." : "After signing in, reload this page.")
            .Append("</p>");
        return page.Append("</main></body></html>").ToString();
    }

    public static string Continue(Uri destination) {
        bool russian = IsRussian();
        string link = HtmlEncoder.Default.Encode(destination.AbsoluteUri);
        return Open(russian, link)
            .Append("<a class=\"button primary standalone-action\" rel=\"noreferrer\" href=\"").Append(link).Append("\">")
            .Append(russian ? "Вернуться в ChatGPT" : "Return to ChatGPT")
            .Append("</a></main></body></html>").ToString();
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
        return page.Append("<style>").Append(Styles).Append("</style></head><body>")
            .Append("<main class=\"consent-card\" aria-labelledby=\"consent-title\">")
            .Append("<div class=\"brand\"><img src=\"/assets/images/header/logo.svg\" width=\"36\" height=\"43\" alt=\"\">FoodDiary</div>")
            .Append("<h1 id=\"consent-title\">").Append(title).Append("</h1>");
    }

    private static void AppendPermission(StringBuilder page, string label) =>
        page.Append("<li><span class=\"permission-mark\" aria-hidden=\"true\"></span>").Append(label).Append("</li>");

    private static void AppendHidden(StringBuilder page, string name, string value) =>
        page.Append("<input type=\"hidden\" name=\"").Append(HtmlEncoder.Default.Encode(name))
            .Append("\" value=\"").Append(HtmlEncoder.Default.Encode(value)).Append("\">");

    private static void AppendButton(StringBuilder page, string classes, string name, string value, string label) =>
        page.Append("<button class=\"").Append(classes).Append("\" type=\"submit\" name=\"").Append(HtmlEncoder.Default.Encode(name))
            .Append("\" value=\"").Append(HtmlEncoder.Default.Encode(value)).Append("\">")
            .Append(label).Append("</button>");
}
