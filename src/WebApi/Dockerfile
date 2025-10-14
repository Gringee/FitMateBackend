# ====== build stage ======
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Skopiuj sln i csproj-y (dostosuj œcie¿ki jeœli u Ciebie inne)
COPY ./FitMateBackend.sln ./
COPY ./src/Domain/Domain.csproj ./src/Domain/
COPY ./src/Application/Application.csproj ./src/Application/
COPY ./src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY ./src/WebApi/WebApi.csproj ./src/WebApi/

RUN dotnet restore ./src/WebApi/WebApi.csproj

# Skopiuj resztê
COPY . .
RUN dotnet publish ./src/WebApi/WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ====== runtime stage ======
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Zmienna œrodowiskowa: nas³uchuj na porcie 8080
ENV ASPNETCORE_URLS=http://+:8080
# Gdy u¿ywasz automigracji (patrz ni¿ej), warto mieæ Production:
ENV DOTNET_ENVIRONMENT=Production

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
