FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.Presentation.Api/*.csproj FoodDiary.Presentation.Api/
COPY FoodDiary.Web.Api/*.csproj FoodDiary.Web.Api/

RUN dotnet restore FoodDiary.Web.Api/FoodDiary.Web.Api.csproj

COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.Presentation.Api/ FoodDiary.Presentation.Api/
COPY FoodDiary.Web.Api/ FoodDiary.Web.Api/

RUN dotnet publish FoodDiary.Web.Api/FoodDiary.Web.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "FoodDiary.Web.Api.dll"]
