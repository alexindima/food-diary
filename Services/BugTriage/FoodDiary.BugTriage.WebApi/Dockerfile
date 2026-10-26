FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:230c3fbae0ceedc70bca04eed2bfb5694e8b248cadbb81010f41b7a993601f55 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:254fe017495996d3abcc487ffacdcce2b73affb380d3af87af12c8dff41fe0b0
WORKDIR /app
COPY --from=build /app/publish .
ENV URLS=http://+:5099
EXPOSE 5099
USER $APP_UID
ENTRYPOINT ["dotnet", "FoodDiary.BugTriage.WebApi.dll"]
