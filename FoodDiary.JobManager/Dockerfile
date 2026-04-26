FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/*.csproj FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.MailInbox.Client/*.csproj FoodDiary.MailInbox.Client/
COPY FoodDiary.MailRelay.Client/*.csproj FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.JobManager/*.csproj FoodDiary.JobManager/

RUN dotnet restore FoodDiary.JobManager/FoodDiary.JobManager.csproj

COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/ FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.MailInbox.Client/ FoodDiary.MailInbox.Client/
COPY FoodDiary.MailRelay.Client/ FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Resources/ FoodDiary.Resources/
COPY FoodDiary.JobManager/ FoodDiary.JobManager/

RUN dotnet publish FoodDiary.JobManager/FoodDiary.JobManager.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs
COPY --from=build /app/publish .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.JobManager.dll"]
