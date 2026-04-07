FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Initializer/*.csproj FoodDiary.Initializer/

RUN dotnet restore FoodDiary.Initializer/FoodDiary.Initializer.csproj

COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Initializer/ FoodDiary.Initializer/

RUN dotnet publish FoodDiary.Initializer/FoodDiary.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache krb5-libs
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.Initializer.dll"]
