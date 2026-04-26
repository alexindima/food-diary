FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY FoodDiary.MailInbox.Domain/*.csproj FoodDiary.MailInbox.Domain/
COPY FoodDiary.MailInbox.Application/*.csproj FoodDiary.MailInbox.Application/
COPY FoodDiary.MailInbox.Infrastructure/*.csproj FoodDiary.MailInbox.Infrastructure/
COPY FoodDiary.MailInbox.Presentation/*.csproj FoodDiary.MailInbox.Presentation/
COPY FoodDiary.MailInbox.WebApi/*.csproj FoodDiary.MailInbox.WebApi/

RUN dotnet restore FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj

COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY FoodDiary.MailInbox.Domain/ FoodDiary.MailInbox.Domain/
COPY FoodDiary.MailInbox.Application/ FoodDiary.MailInbox.Application/
COPY FoodDiary.MailInbox.Infrastructure/ FoodDiary.MailInbox.Infrastructure/
COPY FoodDiary.MailInbox.Presentation/ FoodDiary.MailInbox.Presentation/
COPY FoodDiary.MailInbox.WebApi/ FoodDiary.MailInbox.WebApi/

RUN dotnet publish FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs krb5-libs
COPY --from=build /app/publish .

EXPOSE 5098
EXPOSE 2525
ENV ASPNETCORE_URLS=http://+:5098
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
HEALTHCHECK --interval=10s --timeout=5s --retries=6 --start-period=10s CMD wget -qO- http://127.0.0.1:5098/health/ready >/dev/null || exit 1

ENTRYPOINT ["dotnet", "FoodDiary.MailInbox.WebApi.dll"]
