FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/*.csproj FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.MailInbox.Client/*.csproj FoodDiary.MailInbox.Client/
COPY FoodDiary.MailRelay.Client/*.csproj FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Resources/*.csproj FoodDiary.Resources/
COPY FoodDiary.Initializer/*.csproj FoodDiary.Initializer/

RUN dotnet restore FoodDiary.Initializer/FoodDiary.Initializer.csproj

COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application.Abstractions/ FoodDiary.Application.Abstractions/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.MailInbox.Client/ FoodDiary.MailInbox.Client/
COPY FoodDiary.MailRelay.Client/ FoodDiary.MailRelay.Client/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Resources/ FoodDiary.Resources/
COPY FoodDiary.Initializer/ FoodDiary.Initializer/

RUN dotnet publish FoodDiary.Initializer/FoodDiary.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache krb5-libs
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.Initializer.dll"]
