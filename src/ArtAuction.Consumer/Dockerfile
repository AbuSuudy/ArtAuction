# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /build
COPY ["src/ArtAuction.Consumer/ArtAuction.Consumer.csproj", "src/ArtAuction.Consumer/"]
COPY ["src/ArtAuction.Contract/ArtAuction.Contract.csproj", "src/ArtAuction.Contract/"]
RUN dotnet restore "./src/ArtAuction.Consumer/ArtAuction.Consumer.csproj"
COPY . .
WORKDIR "/build/src/ArtAuction.Consumer"
RUN dotnet build "./ArtAuction.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ArtAuction.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ArtAuction.Consumer.dll"]