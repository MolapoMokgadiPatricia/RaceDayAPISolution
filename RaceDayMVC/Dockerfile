# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so the restore layer is cached between builds
COPY RaceDayMVC.csproj ./
RUN dotnet restore RaceDayMVC.csproj

COPY . ./
RUN dotnet publish RaceDayMVC.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as the non-root user that ships with the .NET 8 images
USER $APP_UID

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiSettings__BaseUrl=http://host.docker.internal:5108

EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDayMVC.dll"]
