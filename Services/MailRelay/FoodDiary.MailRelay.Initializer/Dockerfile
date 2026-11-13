FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS restore

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
FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine@sha256:d39b72ad1534fa0faf52b488bffb0762df7ba269d120f0338713d4dda2a41bf5

WORKDIR /app
RUN apk add --no-cache \
    keyutils-libs=1.6.3-r4 \
    krb5-conf=1.0-r2 \
    krb5-libs=1.22.3-r0 \
    libcom_err=1.47.4-r0 \
    libverto=0.3.2-r2
COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.Initializer.dll"]
