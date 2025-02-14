
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY . ./

RUN dotnet restore

RUN dotnet test TelegramBotTest/TelegramBotTest.csproj --no-restore --verbosity normal

RUN dotnet publish TelegramBot/TelegramBot.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "TelegramBot.dll"]