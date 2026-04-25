FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.MailRelay.Client/*.csproj FoodDiary.MailRelay.Client/
COPY FoodDiary.MailRelay.Application/*.csproj FoodDiary.MailRelay.Application/
COPY FoodDiary.MailRelay.Domain/*.csproj FoodDiary.MailRelay.Domain/
COPY FoodDiary.MailRelay.Infrastructure/*.csproj FoodDiary.MailRelay.Infrastructure/
COPY FoodDiary.MailRelay.Presentation/*.csproj FoodDiary.MailRelay.Presentation/
COPY FoodDiary.MailRelay.WebApi/*.csproj FoodDiary.MailRelay.WebApi/

RUN dotnet restore FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj

COPY FoodDiary.MailRelay.Client/ FoodDiary.MailRelay.Client/
COPY FoodDiary.MailRelay.Application/ FoodDiary.MailRelay.Application/
COPY FoodDiary.MailRelay.Domain/ FoodDiary.MailRelay.Domain/
COPY FoodDiary.MailRelay.Infrastructure/ FoodDiary.MailRelay.Infrastructure/
COPY FoodDiary.MailRelay.Presentation/ FoodDiary.MailRelay.Presentation/
COPY FoodDiary.MailRelay.WebApi/ FoodDiary.MailRelay.WebApi/

RUN dotnet publish FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs
COPY --from=build /app/publish .

EXPOSE 5088
ENV ASPNETCORE_URLS=http://+:5088
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.WebApi.dll"]
