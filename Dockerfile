# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copiar primero los csproj para aprovechar la caché de capas de Docker
COPY ["Meritum.API/Meritum.API.csproj", "Meritum.API/"]
COPY ["Meritum.Core/Meritum.Core.csproj", "Meritum.Core/"]
COPY ["Meritum.Infrastructure/Meritum.Infrastructure.csproj", "Meritum.Infrastructure/"]
RUN dotnet restore "./Meritum.API/Meritum.API.csproj"

# Ahora copiar todo el código fuente y compilar
COPY . .
WORKDIR "/src/Meritum.API"
RUN dotnet build "./Meritum.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Meritum.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Meritum.API.dll"]
