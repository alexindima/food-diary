FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.MailInbox.Domain/*.csproj FoodDiary.MailInbox.Domain/
COPY FoodDiary.MailInbox.Application/*.csproj FoodDiary.MailInbox.Application/
COPY FoodDiary.MailInbox.Infrastructure/*.csproj FoodDiary.MailInbox.Infrastructure/
COPY FoodDiary.MailInbox.Initializer/*.csproj FoodDiary.MailInbox.Initializer/

RUN dotnet restore FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj

COPY FoodDiary.MailInbox.Domain/ FoodDiary.MailInbox.Domain/
COPY FoodDiary.MailInbox.Application/ FoodDiary.MailInbox.Application/
COPY FoodDiary.MailInbox.Infrastructure/ FoodDiary.MailInbox.Infrastructure/
COPY FoodDiary.MailInbox.Initializer/ FoodDiary.MailInbox.Initializer/

RUN dotnet publish FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.MailInbox.Initializer.dll"]
