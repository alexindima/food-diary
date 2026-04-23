FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.MailRelay/*.csproj FoodDiary.MailRelay/

RUN dotnet restore FoodDiary.MailRelay/FoodDiary.MailRelay.csproj

COPY FoodDiary.MailRelay/ FoodDiary.MailRelay/

RUN dotnet publish FoodDiary.MailRelay/FoodDiary.MailRelay.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache icu-libs
COPY --from=build /app/publish .

EXPOSE 5088
ENV ASPNETCORE_URLS=http://+:5088
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "FoodDiary.MailRelay.dll"]
