FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/*.csproj FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.Application.Billing/*.csproj FoodDiary.Application.Billing/
COPY FoodDiary.Application.Marketing/*.csproj FoodDiary.Application.Marketing/
COPY FoodDiary.Application.Users/*.csproj FoodDiary.Application.Users/
COPY FoodDiary.Application.Dietologist/*.csproj FoodDiary.Application.Dietologist/
COPY FoodDiary.Application.Fasting/*.csproj FoodDiary.Application.Fasting/
COPY FoodDiary.Application.Favorites/*.csproj FoodDiary.Application.Favorites/
COPY FoodDiary.Application.Identity/*.csproj FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/*.csproj FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/*.csproj FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/*.csproj FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/*.csproj FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/*.csproj FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/*.csproj FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Exercises/*.csproj FoodDiary.Application.Exercises/
COPY FoodDiary.Application.Tdee/*.csproj FoodDiary.Application.Tdee/
COPY FoodDiary.Application.Hydration/*.csproj FoodDiary.Application.Hydration/
COPY MailInbox/FoodDiary.MailInbox.Client/*.csproj MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/*.csproj MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/*.csproj FoodDiary.Integrations/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.JobManager/*.csproj FoodDiary.JobManager/

RUN dotnet restore FoodDiary.JobManager/FoodDiary.JobManager.csproj

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/ FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.Application.Billing/ FoodDiary.Application.Billing/
COPY FoodDiary.Application.Marketing/ FoodDiary.Application.Marketing/
COPY FoodDiary.Application.Users/ FoodDiary.Application.Users/
COPY FoodDiary.Application.Dietologist/ FoodDiary.Application.Dietologist/
COPY FoodDiary.Application.Fasting/ FoodDiary.Application.Fasting/
COPY FoodDiary.Application.Favorites/ FoodDiary.Application.Favorites/
COPY FoodDiary.Application.Identity/ FoodDiary.Application.Identity/
COPY FoodDiary.Application.MealPlanning/ FoodDiary.Application.MealPlanning/
COPY FoodDiary.Application.RecipeCommunity/ FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/ FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/ FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/ FoodDiary.Application.BodyMetrics/
COPY FoodDiary.Application.Cycles/ FoodDiary.Application.Cycles/
COPY FoodDiary.Application.Exercises/ FoodDiary.Application.Exercises/
COPY FoodDiary.Application.Tdee/ FoodDiary.Application.Tdee/
COPY FoodDiary.Application.Hydration/ FoodDiary.Application.Hydration/
COPY MailInbox/FoodDiary.MailInbox.Client/ MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/ MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/ FoodDiary.Integrations/
COPY FoodDiary.Resources/ FoodDiary.Resources/
COPY FoodDiary.JobManager/ FoodDiary.JobManager/

RUN dotnet publish FoodDiary.JobManager/FoodDiary.JobManager.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs krb5-libs
COPY --from=build /app/publish .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.JobManager.dll"]
