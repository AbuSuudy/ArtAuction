# ---- Base runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser


# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first (better caching)
COPY ["ArtAuction.API/ArtAuction.API.csproj", "ArtAuction.API/"]
COPY ["ArtAuction.Contract/ArtAuction.Contract.csproj", "ArtAuction.Contract/"]

# Restore dependencies
RUN dotnet restore "./ArtAuction.API/ArtAuction.API.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/ArtAuction.API"

# Build
RUN dotnet build "./ArtAuction.API.csproj" -c $BUILD_CONFIGURATION -o /app/build


# ---- Publish stage ----
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./ArtAuction.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


# ---- Final runtime image ----
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ArtAuction.API.dll"]