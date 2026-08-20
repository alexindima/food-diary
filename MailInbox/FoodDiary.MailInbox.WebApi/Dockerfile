FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx NuGet.config ./
COPY .nuget/lockfiles/ .nuget/lockfiles/
COPY FoodDiary.Analyzers/*.csproj FoodDiary.Analyzers/
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/*.csproj MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/*.csproj MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/*.csproj MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Presentation/*.csproj MailInbox/FoodDiary.MailInbox.Presentation/
COPY MailInbox/FoodDiary.MailInbox.WebApi/*.csproj MailInbox/FoodDiary.MailInbox.WebApi/

RUN dotnet restore MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj --locked-mode

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY FoodDiary.Analyzers/ FoodDiary.Analyzers/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/ MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/ MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/ MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Presentation/ MailInbox/FoodDiary.MailInbox.Presentation/
COPY MailInbox/FoodDiary.MailInbox.WebApi/ MailInbox/FoodDiary.MailInbox.WebApi/

RUN dotnet publish MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8

WORKDIR /app
RUN apk add --no-cache \
    icu-data-en=78.1-r0 \
    icu-libs=78.1-r0 \
    keyutils-libs=1.6.3-r4 \
    krb5-conf=1.0-r2 \
    krb5-libs=1.22.2-r1 \
    libcom_err=1.47.4-r0 \
    libverto=0.3.2-r2 \
    su-exec=0.3-r0
COPY --from=build /app/publish .
COPY MailInbox/FoodDiary.MailInbox.WebApi/docker-entrypoint.sh /usr/local/bin/mail-inbox-entrypoint

RUN sed -i 's/\r$//' /usr/local/bin/mail-inbox-entrypoint && \
    chmod 0555 /usr/local/bin/mail-inbox-entrypoint

EXPOSE 5098
EXPOSE 2525
ENV ASPNETCORE_HTTP_PORTS=5098
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
HEALTHCHECK --interval=10s --timeout=5s --retries=6 --start-period=10s CMD if [ "$(id -u)" -eq 0 ]; then exec su-exec "$APP_UID:$APP_UID" wget -qO- http://127.0.0.1:5098/health/ready; else exec wget -qO- http://127.0.0.1:5098/health/ready; fi >/dev/null || exit 1

USER $APP_UID

ENTRYPOINT ["/usr/local/bin/mail-inbox-entrypoint"]
CMD ["dotnet", "FoodDiary.MailInbox.WebApi.dll"]
