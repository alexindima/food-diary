FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:3cc3bbbbf93d82104892f42aa9106b6be4d120346dea0649643a97c801525256 AS restore

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY Services/MailRelay/FoodDiary.MailRelay.Domain/*.csproj Services/MailRelay/FoodDiary.MailRelay.Domain/
COPY Services/MailRelay/FoodDiary.MailRelay.Application/*.csproj Services/MailRelay/FoodDiary.MailRelay.Application/
COPY Services/MailRelay/FoodDiary.MailRelay.Infrastructure/*.csproj Services/MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY Services/MailRelay/FoodDiary.MailRelay.Initializer/*.csproj Services/MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet restore Services/MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj --locked-mode

# Keep restore and source-copy layers below filesystem overlay mount limits.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:3cc3bbbbf93d82104892f42aa9106b6be4d120346dea0649643a97c801525256 AS build
WORKDIR /src
COPY --from=restore /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=restore /src/ /src/
COPY docs/architecture/persistence-technical-sources.txt docs/architecture/

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY Services/MailRelay/FoodDiary.MailRelay.Domain/ Services/MailRelay/FoodDiary.MailRelay.Domain/
COPY Services/MailRelay/FoodDiary.MailRelay.Application/ Services/MailRelay/FoodDiary.MailRelay.Application/
COPY Services/MailRelay/FoodDiary.MailRelay.Infrastructure/ Services/MailRelay/FoodDiary.MailRelay.Infrastructure/
COPY Services/MailRelay/FoodDiary.MailRelay.Initializer/ Services/MailRelay/FoodDiary.MailRelay.Initializer/

RUN dotnet publish Services/MailRelay/FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:f62a272ac1b46e83f56b8ed0416572f31cd1128e2c4a5e63eb34d348e4a36095

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
