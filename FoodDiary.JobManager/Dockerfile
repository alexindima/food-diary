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
COPY Modules/Dietologist/Application/*.csproj Modules/Dietologist/Application/
COPY Modules/Dietologist/Application/Abstractions/*.csproj Modules/Dietologist/Application/Abstractions/
COPY Modules/Dietologist/Domain/*.csproj Modules/Dietologist/Domain/
COPY Modules/Dietologist/Infrastructure/*.csproj Modules/Dietologist/Infrastructure/
COPY Modules/Dietologist/Infrastructure/Model/*.csproj Modules/Dietologist/Infrastructure/Model/
COPY Modules/Fasting/Application/*.csproj Modules/Fasting/Application/
COPY Modules/Fasting/Application/Abstractions/*.csproj Modules/Fasting/Application/Abstractions/
COPY Modules/Fasting/Contracts/*.csproj Modules/Fasting/Contracts/
COPY Modules/Fasting/Domain/*.csproj Modules/Fasting/Domain/
COPY Modules/Fasting/Infrastructure/*.csproj Modules/Fasting/Infrastructure/
COPY Modules/Fasting/Infrastructure/Model/*.csproj Modules/Fasting/Infrastructure/Model/
COPY Modules/Hydration/Contracts/*.csproj Modules/Hydration/Contracts/
COPY Modules/Hydration/Infrastructure/Model/*.csproj Modules/Hydration/Infrastructure/Model/
COPY Modules/Lessons/Domain/*.csproj Modules/Lessons/Domain/
COPY Modules/Lessons/Contracts/*.csproj Modules/Lessons/Contracts/
COPY Modules/Lessons/Application/Abstractions/*.csproj Modules/Lessons/Application/Abstractions/
COPY Modules/Lessons/Application/*.csproj Modules/Lessons/Application/
COPY Modules/Lessons/Infrastructure/*.csproj Modules/Lessons/Infrastructure/
COPY Modules/Lessons/Infrastructure/Model/*.csproj Modules/Lessons/Infrastructure/Model/
COPY Modules/ContentReports/Domain/*.csproj Modules/ContentReports/Domain/
COPY Modules/ContentReports/Contracts/*.csproj Modules/ContentReports/Contracts/
COPY Modules/ContentReports/Infrastructure/Model/*.csproj Modules/ContentReports/Infrastructure/Model/
COPY Modules/Favorites/Application/*.csproj Modules/Favorites/Application/
COPY Modules/Favorites/Domain/*.csproj Modules/Favorites/Domain/
COPY Modules/Favorites/Infrastructure/*.csproj Modules/Favorites/Infrastructure/
COPY Modules/Favorites/Infrastructure/Model/*.csproj Modules/Favorites/Infrastructure/Model/
COPY FoodDiary.Application.Identity/*.csproj FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/*.csproj FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/*.csproj FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/*.csproj FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/*.csproj FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/*.csproj FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/*.csproj FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Dashboard/*.csproj FoodDiary.Application.Dashboard/
COPY FoodDiary.Application.Exercises/*.csproj FoodDiary.Application.Exercises/
COPY Modules/Tdee/Application/*.csproj Modules/Tdee/Application/
COPY FoodDiary.Application.OpenFoodFacts/*.csproj FoodDiary.Application.OpenFoodFacts/
COPY FoodDiary.Application.Products/*.csproj FoodDiary.Application.Products/
COPY FoodDiary.Application.Recipes/*.csproj FoodDiary.Application.Recipes/
COPY Modules/Images/Application/*.csproj Modules/Images/Application/
COPY Modules/Images/Application/Abstractions/*.csproj Modules/Images/Application/Abstractions/
COPY Modules/Images/Infrastructure/*.csproj Modules/Images/Infrastructure/
COPY Modules/Images/Infrastructure/Model/*.csproj Modules/Images/Infrastructure/Model/
COPY FoodDiary.Application.Ai/*.csproj FoodDiary.Application.Ai/
COPY FoodDiary.Application.Statistics/*.csproj FoodDiary.Application.Statistics/
COPY FoodDiary.Application.Meals/*.csproj FoodDiary.Application.Meals/
COPY Modules/WeeklyGoals/Application/*.csproj Modules/WeeklyGoals/Application/
COPY Modules/WeeklyGoals/Application/Abstractions/*.csproj Modules/WeeklyGoals/Application/Abstractions/
COPY Modules/WeeklyGoals/Contracts/*.csproj Modules/WeeklyGoals/Contracts/
COPY Modules/WeeklyGoals/Domain/*.csproj Modules/WeeklyGoals/Domain/
COPY Modules/WeeklyGoals/Infrastructure/*.csproj Modules/WeeklyGoals/Infrastructure/
COPY Modules/WeeklyGoals/Infrastructure/Model/*.csproj Modules/WeeklyGoals/Infrastructure/Model/
COPY FoodDiary.Application.Usda/*.csproj FoodDiary.Application.Usda/
COPY Modules/DailyAdvices/Application/*.csproj Modules/DailyAdvices/Application/
COPY Modules/DailyAdvices/Application/Abstractions/*.csproj Modules/DailyAdvices/Application/Abstractions/
COPY Modules/DailyAdvices/Domain/*.csproj Modules/DailyAdvices/Domain/
COPY Modules/DailyAdvices/Infrastructure/Model/*.csproj Modules/DailyAdvices/Infrastructure/Model/
COPY Modules/Gamification/Application/*.csproj Modules/Gamification/Application/
COPY Modules/Gamification/Application/Abstractions/*.csproj Modules/Gamification/Application/Abstractions/
COPY Modules/Gamification/Domain/*.csproj Modules/Gamification/Domain/
COPY Modules/Gamification/Infrastructure/*.csproj Modules/Gamification/Infrastructure/
COPY Modules/Gamification/Infrastructure/Model/*.csproj Modules/Gamification/Infrastructure/Model/
COPY FoodDiary.Application.Export/*.csproj FoodDiary.Application.Export/
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
COPY Modules/Dietologist/Application/ Modules/Dietologist/Application/
COPY Modules/Dietologist/Application/Abstractions/ Modules/Dietologist/Application/Abstractions/
COPY Modules/Dietologist/Domain/ Modules/Dietologist/Domain/
COPY Modules/Dietologist/Infrastructure/ Modules/Dietologist/Infrastructure/
COPY Modules/Dietologist/Infrastructure/Model/ Modules/Dietologist/Infrastructure/Model/
COPY Modules/Fasting/ Modules/Fasting/
COPY Modules/Fasting/Application/ Modules/Fasting/Application/
COPY Modules/Fasting/Application/Abstractions/ Modules/Fasting/Application/Abstractions/
COPY Modules/Fasting/Contracts/ Modules/Fasting/Contracts/
COPY Modules/Fasting/Domain/ Modules/Fasting/Domain/
COPY Modules/Fasting/Infrastructure/ Modules/Fasting/Infrastructure/
COPY Modules/Fasting/Infrastructure/Model/ Modules/Fasting/Infrastructure/Model/
COPY Modules/Hydration/Contracts/ Modules/Hydration/Contracts/
COPY Modules/Hydration/Infrastructure/Model/ Modules/Hydration/Infrastructure/Model/
COPY Modules/Lessons/Domain/ Modules/Lessons/Domain/
COPY Modules/Lessons/Contracts/ Modules/Lessons/Contracts/
COPY Modules/Lessons/Application/Abstractions/ Modules/Lessons/Application/Abstractions/
COPY Modules/Lessons/Application/ Modules/Lessons/Application/
COPY Modules/Lessons/Infrastructure/ Modules/Lessons/Infrastructure/
COPY Modules/Lessons/Infrastructure/Model/ Modules/Lessons/Infrastructure/Model/
COPY Modules/ContentReports/Domain/ Modules/ContentReports/Domain/
COPY Modules/ContentReports/Contracts/ Modules/ContentReports/Contracts/
COPY Modules/ContentReports/Infrastructure/Model/ Modules/ContentReports/Infrastructure/Model/
COPY Modules/Favorites/ Modules/Favorites/
COPY Modules/Favorites/Application/ Modules/Favorites/Application/
COPY Modules/Favorites/Domain/ Modules/Favorites/Domain/
COPY Modules/Favorites/Infrastructure/ Modules/Favorites/Infrastructure/
COPY Modules/Favorites/Infrastructure/Model/ Modules/Favorites/Infrastructure/Model/
COPY FoodDiary.Application.Identity/ FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/ FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/ FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/ FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/ FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/ FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/ FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Dashboard/ FoodDiary.Application.Dashboard/
COPY FoodDiary.Application.Exercises/ FoodDiary.Application.Exercises/
COPY Modules/Tdee/Application/ Modules/Tdee/Application/
COPY FoodDiary.Application.OpenFoodFacts/ FoodDiary.Application.OpenFoodFacts/
COPY FoodDiary.Application.Products/ FoodDiary.Application.Products/
COPY FoodDiary.Application.Recipes/ FoodDiary.Application.Recipes/
COPY Modules/Images/Application/ Modules/Images/Application/
COPY Modules/Images/Application/Abstractions/ Modules/Images/Application/Abstractions/
COPY Modules/Images/Infrastructure/ Modules/Images/Infrastructure/
COPY Modules/Images/Infrastructure/Model/ Modules/Images/Infrastructure/Model/
COPY FoodDiary.Application.Ai/ FoodDiary.Application.Ai/
COPY FoodDiary.Application.Statistics/ FoodDiary.Application.Statistics/
COPY FoodDiary.Application.Meals/ FoodDiary.Application.Meals/
COPY Modules/WeeklyGoals/ Modules/WeeklyGoals/
COPY Modules/WeeklyGoals/Application/ Modules/WeeklyGoals/Application/
COPY Modules/WeeklyGoals/Application/Abstractions/ Modules/WeeklyGoals/Application/Abstractions/
COPY Modules/WeeklyGoals/Contracts/ Modules/WeeklyGoals/Contracts/
COPY Modules/WeeklyGoals/Domain/ Modules/WeeklyGoals/Domain/
COPY Modules/WeeklyGoals/Infrastructure/ Modules/WeeklyGoals/Infrastructure/
COPY Modules/WeeklyGoals/Infrastructure/Model/ Modules/WeeklyGoals/Infrastructure/Model/
COPY FoodDiary.Application.Usda/ FoodDiary.Application.Usda/
COPY Modules/DailyAdvices/Application/ Modules/DailyAdvices/Application/
COPY Modules/DailyAdvices/Application/Abstractions/ Modules/DailyAdvices/Application/Abstractions/
COPY Modules/DailyAdvices/Domain/ Modules/DailyAdvices/Domain/
COPY Modules/DailyAdvices/Infrastructure/Model/ Modules/DailyAdvices/Infrastructure/Model/
COPY Modules/Gamification/ Modules/Gamification/
COPY Modules/Gamification/Application/ Modules/Gamification/Application/
COPY Modules/Gamification/Application/Abstractions/ Modules/Gamification/Application/Abstractions/
COPY Modules/Gamification/Domain/ Modules/Gamification/Domain/
COPY Modules/Gamification/Infrastructure/ Modules/Gamification/Infrastructure/
COPY Modules/Gamification/Infrastructure/Model/ Modules/Gamification/Infrastructure/Model/
COPY FoodDiary.Application.Export/ FoodDiary.Application.Export/
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
