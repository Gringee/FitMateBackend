# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) kopiujemy ca³oœæ Ÿróde³ (sln + projekty)
COPY src ./src
COPY src/*.sln .

# 2) restore & publish tylko projektu WebApi
RUN dotnet restore src/WebApi/WebApi.csproj
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WebApi.dll"]
