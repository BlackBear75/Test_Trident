# Використовуємо офіційний образ .NET SDK 8 для компіляції та тестування
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копіюємо файли проекту до контейнера
COPY . ./

# Відновлюємо залежності
RUN dotnet restore

# Запускаємо тести
RUN dotnet test TelegramBotTest/TelegramBotTest.csproj --no-restore --verbosity normal

# Публікуємо проект
RUN dotnet publish TelegramBot/TelegramBot.csproj -c Release -o out

# Використовуємо офіційний образ .NET Runtime 8 для запуску
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Вказуємо команду для запуску додатку
ENTRYPOINT ["dotnet", "TelegramBot.dll"]