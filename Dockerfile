# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) kopia sln + csproj
COPY src/*.sln .
COPY src/*/*.csproj ./                # skopiujemy ka¿dy .csproj w src/<proj>/

# 2) restore
RUN for f in */*.csproj; do dotnet restore "$f"; done

# 3) reszta Ÿróde³
COPY src .

# 4) publish tylko WebApi
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app/publish

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WebApi.dll"]
