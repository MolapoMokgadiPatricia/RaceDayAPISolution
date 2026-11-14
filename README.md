# RaceDay -- Event Management Platform

## Project Overview

RaceDay is a web-based event management platform designed to support
running, walking and cycling events in South Africa. The system provides
an API and an MVC web application so that event information and related
functionality can be accessed through the application.

The solution is developed using ASP.NET Core 8 and is organised into
separate API, MVC and test projects.

## Main Features

-   Manage and access sporting event information.
-   Support running, walking and cycling events.
-   Provide an ASP.NET Core Web API.
-   Provide an MVC web interface for users.
-   Use SQL Server for application data storage.
-   Support profile-picture and event-banner uploads.
-   Use Azure Blob Storage for file storage where configured.
-   Include automated build validation through GitHub Actions.
-   Include a test project for automated testing.

> The features available in a running deployment depend on the current
> application configuration and implemented functionality.

## Technology Stack

-   C#
-   .NET 8 / ASP.NET Core
-   ASP.NET Core MVC
-   ASP.NET Core Web API
-   Entity Framework Core
-   Microsoft SQL Server
-   Azure Blob Storage
-   Docker
-   GitHub Actions

## Solution Structure

The solution contains the following main projects:

  Project              Description
  -------------------- -----------------------------------
  `RaceDayAPI`         ASP.NET Core Web API project.
  `RaceDayMVC`         ASP.NET Core MVC web application.
  `RaceDayAPI.Tests`   Automated test project.

The projects are contained in the `RaceDayAPI.sln` solution file.

## Requirements

To build and run the solution locally, install:

-   .NET 8 SDK
-   Microsoft SQL Server
-   Visual Studio 2022 or another .NET-compatible IDE
-   Git

Docker is required only if you intend to run the application using its
Docker configuration. Azure credentials and a storage account are
required only when using Azure Blob Storage.

## Getting Started

### 1. Clone the repository

``` bash
git clone <repository-url>
cd RaceDayAPISolution
```

Replace `<repository-url>` with the URL of this GitHub repository.

### 2. Restore dependencies

``` bash
dotnet restore RaceDayAPI.sln
```

### 3. Configure the database

Configure the SQL Server connection string in the appropriate
application configuration file or user-secrets settings.

The API uses the `RaceDayConnection` connection-string key. Make sure
the configured database server is reachable and that the database
settings match your local environment.

Do not commit passwords, access keys, or other secrets to GitHub.

### 4. Apply database migrations

If Entity Framework Core migrations are included in the project, apply
them using the appropriate startup project and database configuration.
For example:

``` bash
dotnet ef database update --project RaceDayAPI --startup-project RaceDayAPI
```

If the solution uses a different migrations project or startup
configuration, adjust the command accordingly.

### 5. Build the solution

``` bash
dotnet build RaceDayAPI.sln --configuration Release
```

### 6. Run the API

``` bash
dotnet run --project RaceDayAPI
```

The API address and available routes are displayed in the console when
the application starts. If Swagger is enabled in the current
environment, use the Swagger page to explore the available API
endpoints.

### 7. Run the MVC application

``` bash
dotnet run --project RaceDayMVC
```

Use the local URL displayed in the console to open the MVC application
in a browser.

## Running Tests

Run the automated tests with:

``` bash
dotnet test RaceDayAPI.sln --configuration Release
```

The test project is named `RaceDayAPI.Tests`.

## Docker

The repository includes Docker-related configuration for containerised
development or deployment.

To build and run the application with Docker, first check the Dockerfile
and any Docker Compose configuration in the repository. From the
directory containing the relevant Dockerfile, a typical build command
is:

``` bash
docker build -t raceday .
```

Then run the image:

``` bash
docker run -p 8080:8080 raceday
```

The container may require environment variables for the database
connection and any configured storage services. Configure these values
for your environment before running the container. The port mapping may
need to be adjusted to match the port exposed by the Dockerfile.

## Continuous Integration

GitHub Actions is used to automate build validation when changes are
pushed to the repository or a pull request is created.

The workflow is located at:

``` text
.github/workflows/dotnet.yml
```

The workflow restores dependencies, builds the solution and runs the
configured tests. A successful workflow is indicated by a green check
mark on GitHub. If a workflow fails, open the failed run in the
**Actions** tab and review the build or test output to identify the
error.

## Configuration and Security

-   Keep database passwords and cloud-storage credentials out of source
    control.
-   Use environment variables, .NET user secrets, or GitHub Actions
    secrets for sensitive configuration.
-   Configure Azure Blob Storage credentials only in environments where
    that service is required.
-   Do not publish connection strings containing credentials in
    documentation or commit history.

## YouTube Demonstration

A video demonstration of the RaceDay application can be added here:

**YouTube video:** \[Insert the YouTube demonstration link\]

## Current Limitations

The available functionality depends on the current implementation and
configuration. Database connectivity, file uploads, Azure Blob Storage
and Docker execution require the appropriate local or deployment
settings. Some features may require additional configuration before they
can be used outside the development environment.

## References

-   Microsoft. (n.d.). *ASP.NET Core documentation*.
    https://learn.microsoft.com/aspnet/core
-   Microsoft. (n.d.). *.NET documentation*.
    https://learn.microsoft.com/dotnet
-   Microsoft. (n.d.). *Entity Framework Core documentation*.
    https://learn.microsoft.com/ef/core
-   Microsoft. (n.d.). *SQL Server documentation*.
    https://learn.microsoft.com/sql
-   Microsoft. (n.d.). *Azure Blob Storage documentation*.
    https://learn.microsoft.com/azure/storage/blobs
-   Docker. (n.d.). *Docker documentation*. https://docs.docker.com
-   GitHub. (n.d.). *GitHub Actions documentation*.
    https://docs.github.com/actions

## Author

**Molapo Mokgadi Patricia**\
Student Number: **ST10487723**\
Module: **PROG6212**
