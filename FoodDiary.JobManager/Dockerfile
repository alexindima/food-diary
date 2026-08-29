FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/*.csproj FoodDiary.Application.Abstractions/
COPY FoodDiary.Application.Admin/*.csproj FoodDiary.Application.Admin/
COPY FoodDiary.Application.Runtime/*.csproj FoodDiary.Application.Runtime/
COPY FoodDiary.Application.Billing/*.csproj FoodDiary.Application.Billing/
COPY FoodDiary.Application.Marketing/*.csproj FoodDiary.Application.Marketing/
COPY FoodDiary.Application.Users/*.csproj FoodDiary.Application.Users/
COPY FoodDiary.Application.Dietologist/*.csproj FoodDiary.Application.Dietologist/
COPY Modules/Fasting/*.csproj Modules/Fasting/
COPY Modules/Fasting/Application/Abstractions/*.csproj Modules/Fasting/Application/Abstractions/
COPY Modules/Fasting/Contracts/*.csproj Modules/Fasting/Contracts/
COPY Modules/Fasting/Domain/*.csproj Modules/Fasting/Domain/
COPY Modules/Fasting/Infrastructure/*.csproj Modules/Fasting/Infrastructure/
COPY Modules/Fasting/Infrastructure/Model/*.csproj Modules/Fasting/Infrastructure/Model/
COPY FoodDiary.Application.Favorites/*.csproj FoodDiary.Application.Favorites/
COPY FoodDiary.Application.Identity/*.csproj FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/*.csproj FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/*.csproj FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/*.csproj FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/*.csproj FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/*.csproj FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/*.csproj FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Dashboard/*.csproj FoodDiary.Application.Dashboard/
COPY FoodDiary.Application.Exercises/*.csproj FoodDiary.Application.Exercises/
COPY FoodDiary.Application.Tdee/*.csproj FoodDiary.Application.Tdee/
COPY FoodDiary.Application.OpenFoodFacts/*.csproj FoodDiary.Application.OpenFoodFacts/
COPY FoodDiary.Application.Products/*.csproj FoodDiary.Application.Products/
COPY FoodDiary.Application.Recipes/*.csproj FoodDiary.Application.Recipes/
COPY FoodDiary.Application.Images/*.csproj FoodDiary.Application.Images/
COPY FoodDiary.Application.Ai/*.csproj FoodDiary.Application.Ai/
COPY FoodDiary.Application.Lessons/*.csproj FoodDiary.Application.Lessons/
COPY FoodDiary.Application.Statistics/*.csproj FoodDiary.Application.Statistics/
COPY FoodDiary.Application.Meals/*.csproj FoodDiary.Application.Meals/
COPY FoodDiary.Application.WeeklyGoals/*.csproj FoodDiary.Application.WeeklyGoals/
COPY FoodDiary.Application.Usda/*.csproj FoodDiary.Application.Usda/
COPY FoodDiary.Application.WeeklyCheckIn/*.csproj FoodDiary.Application.WeeklyCheckIn/
COPY FoodDiary.Application.DailyAdvices/*.csproj FoodDiary.Application.DailyAdvices/
COPY FoodDiary.Application.ContentReports/*.csproj FoodDiary.Application.ContentReports/
COPY FoodDiary.Application.Gamification/*.csproj FoodDiary.Application.Gamification/
COPY FoodDiary.Application.Export/*.csproj FoodDiary.Application.Export/
COPY FoodDiary.Application.Hydration/*.csproj FoodDiary.Application.Hydration/
COPY MailInbox/FoodDiary.MailInbox.Client/*.csproj MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/*.csproj MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/*.csproj FoodDiary.Integrations/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.JobManager/*.csproj FoodDiary.JobManager/

RUN dotnet restore FoodDiary.JobManager/FoodDiary.JobManager.csproj --locked-mode

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/ FoodDiary.Application.Abstractions/
COPY FoodDiary.Application.Admin/ FoodDiary.Application.Admin/
COPY FoodDiary.Application.Runtime/ FoodDiary.Application.Runtime/
COPY FoodDiary.Application.Billing/ FoodDiary.Application.Billing/
COPY FoodDiary.Application.Marketing/ FoodDiary.Application.Marketing/
COPY FoodDiary.Application.Users/ FoodDiary.Application.Users/
COPY FoodDiary.Application.Dietologist/ FoodDiary.Application.Dietologist/
COPY Modules/Fasting/ Modules/Fasting/
COPY Modules/Fasting/Application/Abstractions/ Modules/Fasting/Application/Abstractions/
COPY Modules/Fasting/Contracts/ Modules/Fasting/Contracts/
COPY Modules/Fasting/Domain/ Modules/Fasting/Domain/
COPY Modules/Fasting/Infrastructure/ Modules/Fasting/Infrastructure/
COPY Modules/Fasting/Infrastructure/Model/ Modules/Fasting/Infrastructure/Model/
COPY FoodDiary.Application.Favorites/ FoodDiary.Application.Favorites/
COPY FoodDiary.Application.Identity/ FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/ FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/ FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/ FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/ FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/ FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/ FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Dashboard/ FoodDiary.Application.Dashboard/
COPY FoodDiary.Application.Exercises/ FoodDiary.Application.Exercises/
COPY FoodDiary.Application.Tdee/ FoodDiary.Application.Tdee/
COPY FoodDiary.Application.OpenFoodFacts/ FoodDiary.Application.OpenFoodFacts/
COPY FoodDiary.Application.Products/ FoodDiary.Application.Products/
COPY FoodDiary.Application.Recipes/ FoodDiary.Application.Recipes/
COPY FoodDiary.Application.Images/ FoodDiary.Application.Images/
COPY FoodDiary.Application.Ai/ FoodDiary.Application.Ai/
COPY FoodDiary.Application.Lessons/ FoodDiary.Application.Lessons/
COPY FoodDiary.Application.Statistics/ FoodDiary.Application.Statistics/
COPY FoodDiary.Application.Meals/ FoodDiary.Application.Meals/
COPY FoodDiary.Application.WeeklyGoals/ FoodDiary.Application.WeeklyGoals/
COPY FoodDiary.Application.Usda/ FoodDiary.Application.Usda/
COPY FoodDiary.Application.WeeklyCheckIn/ FoodDiary.Application.WeeklyCheckIn/
COPY FoodDiary.Application.DailyAdvices/ FoodDiary.Application.DailyAdvices/
COPY FoodDiary.Application.ContentReports/ FoodDiary.Application.ContentReports/
COPY FoodDiary.Application.Gamification/ FoodDiary.Application.Gamification/
COPY FoodDiary.Application.Export/ FoodDiary.Application.Export/
COPY FoodDiary.Application.Hydration/ FoodDiary.Application.Hydration/
COPY MailInbox/FoodDiary.MailInbox.Client/ MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/ MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/ FoodDiary.Integrations/
COPY FoodDiary.Resources/ FoodDiary.Resources/
COPY FoodDiary.JobManager/ FoodDiary.JobManager/

RUN dotnet publish FoodDiary.JobManager/FoodDiary.JobManager.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8

WORKDIR /app
RUN apk add --no-cache \
    icu-data-en=78.1-r0 \
    icu-libs=78.1-r0 \
    keyutils-libs=1.6.3-r4 \
    krb5-conf=1.0-r2 \
    krb5-libs=1.22.2-r1 \
    libcom_err=1.47.4-r0 \
    libverto=0.3.2-r2
COPY --from=build /app/publish .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.JobManager.dll"]
