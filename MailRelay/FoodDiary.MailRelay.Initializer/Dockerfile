FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY MailRelay/FoodDiary.MailRelay.Domain/*.csproj MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Application/*.csproj MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/*.csproj MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Initializer/*.csproj MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet restore MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj

COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY MailRelay/FoodDiary.MailRelay.Domain/ MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Application/ MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/ MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Initializer/ MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet publish MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache krb5-libs
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.Initializer.dll"]
