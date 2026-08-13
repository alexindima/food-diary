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
COPY FoodDiary.Application.RecipeCommunity/*.csproj FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/*.csproj FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/*.csproj FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/*.csproj FoodDiary.Application.BodyMetrics/
COPY MailInbox/FoodDiary.MailInbox.Client/*.csproj MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/*.csproj MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/*.csproj FoodDiary.Integrations/
COPY FoodDiary.Presentation.Api/*.csproj FoodDiary.Presentation.Api/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.Web.Api/*.csproj FoodDiary.Web.Api/

RUN dotnet restore FoodDiary.Web.Api/FoodDiary.Web.Api.csproj

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
COPY FoodDiary.Application.RecipeCommunity/ FoodDiary.Application.RecipeCommunity/
COPY FoodDiary.Application.Wearables/ FoodDiary.Application.Wearables/
COPY FoodDiary.Application.Notifications/ FoodDiary.Application.Notifications/
COPY FoodDiary.Application.BodyMetrics/ FoodDiary.Application.BodyMetrics/
COPY MailInbox/FoodDiary.MailInbox.Client/ MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/ MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/ FoodDiary.Integrations/
COPY FoodDiary.Presentation.Api/ FoodDiary.Presentation.Api/
COPY FoodDiary.Resources/ FoodDiary.Resources/
COPY FoodDiary.Web.Api/ FoodDiary.Web.Api/

RUN dotnet publish FoodDiary.Web.Api/FoodDiary.Web.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs krb5-libs
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_HTTP_PORTS=5000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.Web.Api.dll"]
