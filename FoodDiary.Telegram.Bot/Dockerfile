FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:230c3fbae0ceedc70bca04eed2bfb5694e8b248cadbb81010f41b7a993601f55 AS restore

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY FoodDiary.Telegram.Bot/*.csproj FoodDiary.Telegram.Bot/

RUN dotnet restore FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj --locked-mode

# Keep restore and source-copy layers below filesystem overlay mount limits.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:230c3fbae0ceedc70bca04eed2bfb5694e8b248cadbb81010f41b7a993601f55 AS build
WORKDIR /src
COPY --from=restore /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=restore /src/ /src/
COPY docs/architecture/persistence-technical-sources.txt docs/architecture/

COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY FoodDiary.Telegram.Bot/ FoodDiary.Telegram.Bot/

RUN dotnet publish FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:f62a272ac1b46e83f56b8ed0416572f31cd1128e2c4a5e63eb34d348e4a36095

WORKDIR /app
RUN apk add --no-cache \
    icu-data-en=78.1-r0 \
    icu-libs=78.1-r0
COPY --from=build /app/publish .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.Telegram.Bot.dll"]
