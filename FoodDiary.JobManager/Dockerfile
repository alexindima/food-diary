FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY Directory.Build.props FoodDiary.slnx ./
COPY FoodDiary.Domain/*.csproj FoodDiary.Domain/
COPY FoodDiary.Application/*.csproj FoodDiary.Application/
COPY FoodDiary.Infrastructure/*.csproj FoodDiary.Infrastructure/
COPY FoodDiary.JobManager/*.csproj FoodDiary.JobManager/

RUN dotnet restore FoodDiary.JobManager/FoodDiary.JobManager.csproj

COPY FoodDiary.Domain/ FoodDiary.Domain/
COPY FoodDiary.Application/ FoodDiary.Application/
COPY FoodDiary.Infrastructure/ FoodDiary.Infrastructure/
COPY FoodDiary.JobManager/ FoodDiary.JobManager/

RUN dotnet publish FoodDiary.JobManager/FoodDiary.JobManager.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.JobManager.dll"]
