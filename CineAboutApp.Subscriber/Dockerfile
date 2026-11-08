FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "CineAboutApp.Subscriber/CineAboutApp.Subscriber.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .

# Firebase credentials are read from the FIREBASE_CREDENTIALS_JSON environment variable
ENTRYPOINT ["dotnet", "CineAboutApp.Subscriber.dll"]
