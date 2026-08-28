FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY MailRelay/FoodDiary.MailRelay.Domain/*.csproj MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Application/*.csproj MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/*.csproj MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Initializer/*.csproj MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet restore MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj --locked-mode

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY MailRelay/FoodDiary.MailRelay.Domain/ MailRelay/FoodDiary.MailRelay.Domain/
COPY MailRelay/FoodDiary.MailRelay.Application/ MailRelay/FoodDiary.MailRelay.Application/
COPY MailRelay/FoodDiary.MailRelay.Infrastructure/ MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY MailRelay/FoodDiary.MailRelay.Initializer/ MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet publish MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8

WORKDIR /app
RUN apk add --no-cache \
    keyutils-libs=1.6.3-r4 \
    krb5-conf=1.0-r2 \
    krb5-libs=1.22.2-r1 \
    libcom_err=1.47.4-r0 \
    libverto=0.3.2-r2
COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.Initializer.dll"]
