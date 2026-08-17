FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY FoodDiary.Telegram.Bot/*.csproj FoodDiary.Telegram.Bot/

RUN dotnet restore FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj --locked-mode

COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY FoodDiary.Telegram.Bot/ FoodDiary.Telegram.Bot/

RUN dotnet publish FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8

WORKDIR /app
RUN apk add --no-cache \
    icu-data-en=78.1-r0 \
    icu-libs=78.1-r0
COPY --from=build /app/publish .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.Telegram.Bot.dll"]
