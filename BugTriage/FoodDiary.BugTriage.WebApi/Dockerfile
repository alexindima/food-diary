FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build
WORKDIR /src
COPY .editorconfig Directory.Build.props Directory.Packages.props NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY docs/architecture/persistence-technical-sources.txt docs/architecture/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY MailInbox/FoodDiary.MailInbox.Client/*.csproj MailInbox/FoodDiary.MailInbox.Client/
COPY BugTriage/FoodDiary.BugTriage.Application/*.csproj BugTriage/FoodDiary.BugTriage.Application/
COPY BugTriage/FoodDiary.BugTriage.Infrastructure/*.csproj BugTriage/FoodDiary.BugTriage.Infrastructure/
COPY BugTriage/FoodDiary.BugTriage.Presentation/*.csproj BugTriage/FoodDiary.BugTriage.Presentation/
COPY BugTriage/FoodDiary.BugTriage.WebApi/*.csproj BugTriage/FoodDiary.BugTriage.WebApi/
RUN dotnet restore BugTriage/FoodDiary.BugTriage.WebApi --locked-mode
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY MailInbox/FoodDiary.MailInbox.Client/ MailInbox/FoodDiary.MailInbox.Client/
COPY BugTriage/FoodDiary.BugTriage.Application/ BugTriage/FoodDiary.BugTriage.Application/
COPY BugTriage/FoodDiary.BugTriage.Infrastructure/ BugTriage/FoodDiary.BugTriage.Infrastructure/
COPY BugTriage/FoodDiary.BugTriage.Presentation/ BugTriage/FoodDiary.BugTriage.Presentation/
COPY BugTriage/FoodDiary.BugTriage.WebApi/ BugTriage/FoodDiary.BugTriage.WebApi/
RUN dotnet publish BugTriage/FoodDiary.BugTriage.WebApi -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8
WORKDIR /app
COPY --from=build /app/publish .
ENV URLS=http://+:5099
EXPOSE 5099
USER $APP_UID
ENTRYPOINT ["dotnet", "FoodDiary.BugTriage.WebApi.dll"]
