# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the API csproj and restore dependencies
COPY ["RaceDayAPI/RaceDayAPI.csproj", "RaceDayAPI/"]
RUN dotnet restore "RaceDayAPI/RaceDayAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/RaceDayAPI"
RUN dotnet publish "RaceDayAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (default for .NET 8 containers)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RaceDayAPI.dll"]