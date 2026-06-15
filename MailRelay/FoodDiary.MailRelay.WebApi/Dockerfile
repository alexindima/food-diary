FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY MailRelay/FoodDiary.MailRelay.Client/*.csproj MailRelay/FoodDiary.MailRelay.Client/
COPY MailRelay/FoodDiary.MailRelay.Application/*.csproj MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Domain/*.csproj MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/*.csproj MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Presentation/*.csproj MailRelay/FoodDiary.MailRelay.Presentation/
COPY MailRelay/FoodDiary.MailRelay.WebApi/*.csproj MailRelay/FoodDiary.MailRelay.WebApi/

RUN dotnet restore MailRelay/FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj

COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY MailRelay/FoodDiary.MailRelay.Client/ MailRelay/FoodDiary.MailRelay.Client/
COPY MailRelay/FoodDiary.MailRelay.Application/ MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Domain/ MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/ MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Presentation/ MailRelay/FoodDiary.MailRelay.Presentation/
COPY MailRelay/FoodDiary.MailRelay.WebApi/ MailRelay/FoodDiary.MailRelay.WebApi/

RUN dotnet publish MailRelay/FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs krb5-libs
COPY --from=build /app/publish .

EXPOSE 5088
ENV ASPNETCORE_HTTP_PORTS=5088
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.WebApi.dll"]
