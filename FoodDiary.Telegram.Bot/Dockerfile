FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props ./
COPY FoodDiary.Telegram.Bot/*.csproj FoodDiary.Telegram.Bot/

RUN dotnet restore FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj

COPY FoodDiary.Telegram.Bot/ FoodDiary.Telegram.Bot/

RUN dotnet publish FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.Telegram.Bot.dll"]
