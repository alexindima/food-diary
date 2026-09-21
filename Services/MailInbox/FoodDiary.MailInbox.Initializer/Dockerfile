FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:4ac537e13e2f55d1d588ed3e618cb0cb6b82dd8deb17830de43d5086fbde958b AS restore

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY Services/MailInbox/FoodDiary.MailInbox.Domain/*.csproj Services/MailInbox/FoodDiary.MailInbox.Domain/
COPY Services/MailInbox/FoodDiary.MailInbox.Application/*.csproj Services/MailInbox/FoodDiary.MailInbox.Application/
COPY Services/MailInbox/FoodDiary.MailInbox.Infrastructure/*.csproj Services/MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY Services/MailInbox/FoodDiary.MailInbox.Initializer/*.csproj Services/MailInbox/FoodDiary.MailInbox.Initializer/

RUN dotnet restore Services/MailInbox/FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj --locked-mode

# Keep restore and source-copy layers below filesystem overlay mount limits.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:4ac537e13e2f55d1d588ed3e618cb0cb6b82dd8deb17830de43d5086fbde958b AS build
WORKDIR /src
COPY --from=restore /root/.nuget/packages/ /root/.nuget/packages/
COPY --from=restore /src/ /src/
COPY docs/architecture/persistence-technical-sources.txt docs/architecture/

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY Services/MailInbox/FoodDiary.MailInbox.Domain/ Services/MailInbox/FoodDiary.MailInbox.Domain/
COPY Services/MailInbox/FoodDiary.MailInbox.Application/ Services/MailInbox/FoodDiary.MailInbox.Application/
COPY Services/MailInbox/FoodDiary.MailInbox.Infrastructure/ Services/MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY Services/MailInbox/FoodDiary.MailInbox.Initializer/ Services/MailInbox/FoodDiary.MailInbox.Initializer/

RUN dotnet publish Services/MailInbox/FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj -c Release -o /app/publish --no-restore

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

ENTRYPOINT ["dotnet", "FoodDiary.MailInbox.Initializer.dll"]
