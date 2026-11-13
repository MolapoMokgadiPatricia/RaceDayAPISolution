# syntax=docker/dockerfile:1
# Optional: runs the Part 2 API in a container (used by docker-compose.yml).
# Build from the repository root:  docker build -f RaceDayAPI.Dockerfile -t raceday-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDayAPI/RaceDayAPI.csproj RaceDayAPI/
RUN dotnet restore RaceDayAPI/RaceDayAPI.csproj
COPY RaceDayAPI/ RaceDayAPI/
RUN dotnet publish RaceDayAPI/RaceDayAPI.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDayAPI.dll"]
