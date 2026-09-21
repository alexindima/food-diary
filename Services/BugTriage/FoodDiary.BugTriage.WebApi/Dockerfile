FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:4ac537e13e2f55d1d588ed3e618cb0cb6b82dd8deb17830de43d5086fbde958b AS build
WORKDIR /src
COPY .editorconfig Directory.Build.props Directory.Packages.props NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY docs/architecture/persistence-technical-sources.txt docs/architecture/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Services/MailInbox/FoodDiary.MailInbox.Client/*.csproj Services/MailInbox/FoodDiary.MailInbox.Client/
COPY Services/BugTriage/FoodDiary.BugTriage.Application/*.csproj Services/BugTriage/FoodDiary.BugTriage.Application/
COPY Services/BugTriage/FoodDiary.BugTriage.Infrastructure/*.csproj Services/BugTriage/FoodDiary.BugTriage.Infrastructure/
COPY Services/BugTriage/FoodDiary.BugTriage.Presentation/*.csproj Services/BugTriage/FoodDiary.BugTriage.Presentation/
COPY Services/BugTriage/FoodDiary.BugTriage.WebApi/*.csproj Services/BugTriage/FoodDiary.BugTriage.WebApi/
RUN dotnet restore Services/BugTriage/FoodDiary.BugTriage.WebApi --locked-mode
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Services/MailInbox/FoodDiary.MailInbox.Client/ Services/MailInbox/FoodDiary.MailInbox.Client/
COPY Services/BugTriage/FoodDiary.BugTriage.Application/ Services/BugTriage/FoodDiary.BugTriage.Application/
COPY Services/BugTriage/FoodDiary.BugTriage.Infrastructure/ Services/BugTriage/FoodDiary.BugTriage.Infrastructure/
COPY Services/BugTriage/FoodDiary.BugTriage.Presentation/ Services/BugTriage/FoodDiary.BugTriage.Presentation/
COPY Services/BugTriage/FoodDiary.BugTriage.WebApi/ Services/BugTriage/FoodDiary.BugTriage.WebApi/
RUN dotnet publish Services/BugTriage/FoodDiary.BugTriage.WebApi -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:f62a272ac1b46e83f56b8ed0416572f31cd1128e2c4a5e63eb34d348e4a36095
WORKDIR /app
COPY --from=build /app/publish .
ENV URLS=http://+:5099
EXPOSE 5099
USER $APP_UID
ENTRYPOINT ["dotnet", "FoodDiary.BugTriage.WebApi.dll"]
