using FoodDiary.Telegram.Bot;
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
    .ValidateOnStart();

builder.Services.AddHttpClient();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<ITelegramBotClient>(sp => {
    TelegramBotOptions options = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services.AddHostedService<TelegramBotWorker>();

IHost app = builder.Build();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
