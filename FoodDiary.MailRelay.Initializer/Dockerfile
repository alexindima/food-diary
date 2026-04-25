FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.MailRelay.Domain/*.csproj FoodDiary.MailRelay.Domain/
COPY FoodDiary.MailRelay.Application/*.csproj FoodDiary.MailRelay.Application/
COPY FoodDiary.MailRelay.Infrastructure/*.csproj FoodDiary.MailRelay.Infrastructure/
COPY FoodDiary.MailRelay.Initializer/*.csproj FoodDiary.MailRelay.Initializer/

RUN dotnet restore FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj

COPY FoodDiary.MailRelay.Domain/ FoodDiary.MailRelay.Domain/
COPY FoodDiary.MailRelay.Application/ FoodDiary.MailRelay.Application/
COPY FoodDiary.MailRelay.Infrastructure/ FoodDiary.MailRelay.Infrastructure/
COPY FoodDiary.MailRelay.Initializer/ FoodDiary.MailRelay.Initializer/

RUN dotnet publish FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.Initializer.dll"]
