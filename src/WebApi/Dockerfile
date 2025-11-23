FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./FitMateBackend.sln ./
COPY ./src/Domain/Domain.csproj ./src/Domain/
COPY ./src/Application/Application.csproj ./src/Application/
COPY ./src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY ./src/WebApi/WebApi.csproj ./src/WebApi/

RUN dotnet restore ./src/WebApi/WebApi.csproj

COPY . .
RUN dotnet publish ./src/WebApi/WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app


COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
