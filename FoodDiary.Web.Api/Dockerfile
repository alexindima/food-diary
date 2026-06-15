FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/*.csproj FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY MailInbox/FoodDiary.MailInbox.Client/*.csproj MailInbox/FoodDiary.MailInbox.Client/
COPY MailRelay/FoodDiary.MailRelay.Client/*.csproj MailRelay/FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Integrations/*.csproj FoodDiary.Integrations/
COPY FoodDiary.Presentation.Api/*.csproj FoodDiary.Presentation.Api/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.Web.Api/*.csproj FoodDiary.Web.Api/

RUN dotnet restore FoodDiary.Web.Api/FoodDiary.Web.Api.csproj

COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/ FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/ FoodDiary.Application/
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
