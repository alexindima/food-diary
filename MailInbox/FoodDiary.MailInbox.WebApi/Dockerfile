FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/*.csproj MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/*.csproj MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/*.csproj MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Presentation/*.csproj MailInbox/FoodDiary.MailInbox.Presentation/
COPY MailInbox/FoodDiary.MailInbox.WebApi/*.csproj MailInbox/FoodDiary.MailInbox.WebApi/

RUN dotnet restore MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/ MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/ MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/ MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Presentation/ MailInbox/FoodDiary.MailInbox.Presentation/
COPY MailInbox/FoodDiary.MailInbox.WebApi/ MailInbox/FoodDiary.MailInbox.WebApi/

RUN dotnet publish MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs krb5-libs
COPY --from=build /app/publish .

EXPOSE 5098
EXPOSE 2525
ENV ASPNETCORE_HTTP_PORTS=5098
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
HEALTHCHECK --interval=10s --timeout=5s --retries=6 --start-period=10s CMD wget -qO- http://127.0.0.1:5098/health/ready >/dev/null || exit 1

ENTRYPOINT ["dotnet", "FoodDiary.MailInbox.WebApi.dll"]
