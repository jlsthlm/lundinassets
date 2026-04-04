# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LundinAssets.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Ensure upload directories exist inside the image
# (they will be overridden by the Docker volume mount)
RUN mkdir -p wwwroot/uploads/assets wwwroot/uploads/profiles

# .NET 8 containers default to port 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LundinAssets.dll"]
