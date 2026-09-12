using FoodDiary.Telegram.Bot;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<TelegramBotOptions>()
    .Bind(builder.Configuration.GetSection(TelegramBotOptions.SectionName))
    .Validate(TelegramBotOptions.HasValidWebAppUrl,
        "TelegramBot:WebAppUrl must be empty or an absolute URL.")
    .Validate(TelegramBotOptions.HasValidApiBaseUrl,
        "TelegramBot:ApiBaseUrl must be empty or an absolute URL.")
    .Validate(TelegramBotOptions.HasValidApiSecret,
        "TelegramBot:ApiSecret must be empty or at least 16 characters long.")
    .Validate(TelegramBotOptions.HasOperationConfiguration,
        "Enabled Telegram operations require Token, ApiSecret, an API base URL and an HTTPS WebAppUrl without credentials, query or fragment.")
    .ValidateOnStart();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(BotOperationClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient(BotDiaryClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(60))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<ITelegramBotClient>(sp => {
    TelegramBotOptions options = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddHostedService<TelegramOperationWorker>();

IHost app = builder.Build();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
