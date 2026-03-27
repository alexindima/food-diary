using FoodDiary.Telegram.Bot;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<TelegramBotOptions>()
    .Bind(builder.Configuration.GetSection(TelegramBotOptions.SectionName))
    .Validate(static options => TelegramBotOptions.HasValidWebAppUrl(options.WebAppUrl),
        "TelegramBot:WebAppUrl must be empty or an absolute URL.")
    .Validate(static options => TelegramBotOptions.HasValidApiBaseUrl(options.ApiBaseUrl),
        "TelegramBot:ApiBaseUrl must be empty or an absolute URL.")
    .Validate(static options => TelegramBotOptions.HasValidApiSecret(options.ApiSecret),
        "TelegramBot:ApiSecret must be empty or at least 16 characters long.")
    .ValidateOnStart();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ITelegramBotClient>(sp => {
    var options = sp.GetRequiredService<IOptions<TelegramBotOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services.AddHostedService<TelegramBotWorker>();

var app = builder.Build();

await app.RunAsync();
