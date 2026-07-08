FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY .editorconfig Directory.Build.props Directory.Packages.props FoodDiary.slnx ./
COPY Shared/FoodDiary.Domain.Primitives/*.csproj Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/*.csproj Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/*.csproj Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/*.csproj MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/*.csproj MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/*.csproj MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Initializer/*.csproj MailInbox/FoodDiary.MailInbox.Initializer/

RUN dotnet restore MailInbox/FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj

COPY Shared/FoodDiary.Domain.Primitives/ Shared/FoodDiary.Domain.Primitives/
COPY Shared/FoodDiary.Mediator/ Shared/FoodDiary.Mediator/
COPY Shared/FoodDiary.Results/ Shared/FoodDiary.Results/
COPY MailInbox/FoodDiary.MailInbox.Domain/ MailInbox/FoodDiary.MailInbox.Domain/
COPY MailInbox/FoodDiary.MailInbox.Application/ MailInbox/FoodDiary.MailInbox.Application/
COPY MailInbox/FoodDiary.MailInbox.Infrastructure/ MailInbox/FoodDiary.MailInbox.Infrastructure/
COPY MailInbox/FoodDiary.MailInbox.Initializer/ MailInbox/FoodDiary.MailInbox.Initializer/

RUN dotnet publish MailInbox/FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
RUN apk add --no-cache krb5-libs
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodDiary.MailInbox.Initializer.dll"]
