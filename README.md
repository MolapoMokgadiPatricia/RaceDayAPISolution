RaceDay

RaceDay CI

RaceDay is a full-stack event management platform for South African road running, walking and cycling events. Organisers create events, set up categories, confirm entries and publish results. Participants browse events, enter in a few taps and keep a personal race history.

The Portfolio of Evidence was built in three parts, each on top of the previous one:

Part	What it delivers
Part 1	System planning: ERD, API endpoint plan and SQL Server script (in /docs)
Part 2	RaceDayAPI: ASP.NET Core 8 Web API with Entity Framework Core, session-based authentication, role-based access, Swagger and xUnit tests
Part 3	RaceDayMVC: ASP.NET Core MVC website that consumes the API, Azure Blob Storage image uploads, Docker and CI/CD
Student details
Author: Molapo Mokgadi Patricia
Student number: ST10487723
Module: PROG6212 - Programming 2B
GitHub: @MolapoMokgadiPatricia
Technology stack
ASP.NET Core 8 (Web API and MVC), C#
Microsoft SQL Server with Entity Framework Core 8 (Code-First)
Session-based authentication with hashed passwords
Azure Blob Storage (Azurite emulator for development) for event banners and profile pictures
Bootstrap 5, custom CSS (teal blue, white and cream theme) and JavaScript
Swagger / OpenAPI, xUnit
Docker and GitHub Actions
How the system fits together
 Browser  -->  RaceDayMVC (website)  -->  RaceDayAPI (REST API)  -->  SQL Server
                                                |
                                                +--------------->  Azure Blob Storage
                                                                   (event banners, profile pictures)
RaceDayMVC has no database access and no business logic. Every action goes through Services/RaceDayApiClient.cs.
On login, the API's session cookie is kept in the MVC server-side session together with the user id and role, and is forwarded on every API call.
The MVC site never talks to Azure. Uploaded images are sent to the API, which stores them in Blob Storage and saves the URL in the database.
Roles
Role	What they can do
Organiser	Create, edit and delete events (with a banner image), define categories, view enrolments and confirm or decline them, capture and publish finish times and positions
Participant	Browse and filter events, enter an event by choosing a category, see their entries and statuses, see their race history, edit their profile and add a profile picture

Roles are enforced in the API with [AuthorizeRole] and again in the MVC site with [SessionAuthorize]. Each role has its own navigation menu, visitors who are not logged in are sent to the login page, and the wrong role sees an access-denied page.

Website pages
Area	Pages
Everyone	Home (hero and live-filtered upcoming events), Events, Event details, Login, Register
Participant	Event details with Enter event and category choice, My events (colour-coded status, cancel entry), My results (event, date, category, finish time and position such as "47th out of 312 finishers"), Profile with picture upload
Organiser	Dashboard (events, enrolments per event, upcoming dates), My events (create, edit, delete, banner upload), Categories, Enrolments (confirm or decline), Results entry (with "Rank by finish time")

Enrolment status colours: Confirmed (API value Approved) green, Pending amber, Rejected red, Cancelled grey.

Project structure
RaceDayAPISolution
|-- .github/workflows/dotnet.yml    CI pipeline
|-- docs/                           ERD, SQL script (Part 1)
|-- RaceDayAPI/                     Web API (Part 2)
|   |-- Controllers/                Auth, Users, Events, Categories, Enrolments, Results, EventBanners, ProfilePictures
|   |-- Models/  Data/              Entities and EF Core context
|   |-- Services/                   Password hashing, Blob Storage, image validation
|   `-- Attributes/ Extensions/     Role attribute and session helpers
|-- RaceDayAPI.Tests/               xUnit tests
|-- RaceDayMVC/                     Website (Part 3)
|   |-- Controllers/ Views/         Account, Home, Events, Participant, Organiser
|   |-- Services/ Filters/          API client, session keys, role filter
|   |-- Models/ Helpers/            API models, view models, display helpers
|   |-- wwwroot/                    CSS, JavaScript, Bootstrap, images
|   `-- Dockerfile
|-- docker-compose.yml              Optional: SQL Server + API + website
`-- RaceDayAPI.sln
Getting started
Prerequisites
.NET 8 SDK
SQL Server or SQL Server Express
Azurite (built into Visual Studio) or an Azure Storage account
Docker Desktop (only for the Docker steps)
1. Database

Set ConnectionStrings:RaceDayConnection in RaceDayAPI/appsettings.json (or in user secrets) to your SQL Server. The API creates the RaceDayDB database and tables on first run. docs/RaceDayDatabase.sql contains the same schema with sample data if you prefer to create it in SSMS.

2. Blob storage

appsettings.json points the API at the Azurite emulator with UseDevelopmentStorage=true. Start Azurite from Visual Studio, or run:

docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
3. Run the API

Run RaceDayAPI from Visual Studio, or dotnet run --project RaceDayAPI --launch-profile https.

Profile	URL
https	https://localhost:7143
http	http://localhost:5109

Swagger opens at /swagger in the Development environment (the default when you run from Visual Studio), for example https://localhost:7143/swagger.

4. Run the website

Run RaceDayMVC from Visual Studio, or dotnet run --project RaceDayMVC --launch-profile https.

Profile	URL
https	https://localhost:7200
http	http://localhost:5200

The website finds the API through ApiSettings:BaseUrl in RaceDayMVC/appsettings.json (default https://localhost:7143).

5. Create accounts

Open the website, choose Sign up, and create one Organiser and one Participant account. Participants can add a profile picture while registering or later on their profile page.

API endpoints

All endpoints are visible and testable in Swagger. "Any" means any logged-in user.

Area	Method and route	Role
Authentication	POST /api/auth/register, POST /api/auth/login, POST /api/auth/logout	Public
Authentication	GET /api/auth/me	Any
Users	GET /api/users	Organiser
Users	GET /api/users/{id}, PUT /api/users/{id}	Any (Participants: own profile only)
Events	GET /api/events, GET /api/events/{id}	Public
Events	POST /api/events, PUT /api/events/{id}, DELETE /api/events/{id}	Organiser
Categories	GET /api/categories, GET /api/categories/{id}, GET /api/categories/event/{eventId}	Public
Categories	POST /api/categories, PUT /api/categories/{id}, DELETE /api/categories/{id}	Organiser
Enrolments	GET /api/enrolments, GET /api/enrolments/event/{eventId}	Organiser
Enrolments	GET /api/enrolments/{id}, GET /api/enrolments/participant/{participantId}, PUT /api/enrolments/{id}, DELETE /api/enrolments/{id}	Any (own enrolments for Participants)
Enrolments	POST /api/enrolments	Participant
Results	GET /api/results, GET /api/results/{id}, GET /api/results/enrolment/{enrolmentId}	Any
Results	GET /api/results/my-history	Participant
Results	POST /api/results, PUT /api/results/{id}, DELETE /api/results/{id}	Organiser
Uploads	POST /api/EventBanners/{id}/banner (own event), GET /api/EventBanners/storage-test	Organiser
Uploads	POST /api/ProfilePictures	Participant
Azure Blob Storage
The containers event-banners and profile-pictures are created automatically with public read access for blobs.
To use a real storage account, give the API its connection string without committing it:
cd RaceDayAPI
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AzureBlobStorage" "<your connection string>"

Then turn on "Allow Blob anonymous access" for the storage account, otherwise the images will not display.

Images must be JPEG, PNG or WebP and no larger than 5 MB. The API checks the size, content type, extension and the real file signature.
Background picture

The home page hero uses RaceDayMVC/wwwroot/img/images.jpg as its background when the file exists, under a teal overlay so the text stays readable. Without it the hero falls back to a teal gradient.

Docker

The website runs in a container. With the API running on your computer, start the API with its http profile (port 5109), then run:

docker build -t raceday-mvc ./RaceDayMVC
docker run --rm -p 5200:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5109 raceday-mvc

Open http://localhost:5200. On Linux, add --add-host=host.docker.internal:host-gateway to the docker run command.

The Dockerfile uses a multi-stage build: the .NET SDK image restores and publishes the app, and only the published output is copied into the smaller ASP.NET runtime image. The project file is restored in its own layer so repeat builds are fast.

Optional: docker compose up --build starts SQL Server, the API and the website together. Set SA_PASSWORD and AZURE_STORAGE_CONNECTION_STRING first. In that setup the website is at http://localhost:5200 and the API at http://localhost:5108/swagger.

Unit tests
dotnet test RaceDayAPI.Tests/RaceDayAPI.Tests.csproj

The xUnit tests cover registration and login, event management by Organisers, role-based rejection of the wrong role, category management, enrolments, results, password hashing and the image upload endpoints (ownership, real image content, missing files, missing participant).

CI/CD

The GitHub Actions workflow in .github/workflows/dotnet.yml runs on every push and pull request with three jobs:

Validate repository structure: checks that /docs contains the ERD and SQL script, that the API, test and MVC projects, the Dockerfile and this README exist, and that the README has the Docker run command and a YouTube section. It also reports the commit count.
Build solution and run unit tests: restores and builds the API, the tests and the MVC project in Release mode, runs all unit tests and uploads the test results.
Build and start MVC Docker image: builds the image, starts the container and checks that it serves the login page.
Green build
<img width="655" height="161" alt="Green GitHub Actions build" src="https://github.com/user-attachments/assets/f3ea904d-003f-4c70-9f27-2e41759b90ed" />
Video

(paste your unlisted YouTube link here)

Known limitations
Only Participants have a profile picture. Organisers show their initials, because the database has no picture column for them.
The API stores enrolment status Approved, which the website shows as Confirmed.
Deleting an event that already has enrolments is blocked, and the website explains why.
AI tool use

Claude (Anthropic) was used for structural syntax refinement, environment review cycles, logic debugging assistance and verification alignment in Part 2. In Part 3 it was also used to help build the MVC views, stylesheet and JavaScript, the image upload endpoints, the Dockerfile and the CI workflow.

References
Anthropic, 2026. Claude AI assistant [Online]. Available at: https://claude.ai [Accessed 15 October 2026].
Bootstrap, 2026. Bootstrap 5.3 documentation [Online]. Available at: https://getbootstrap.com/docs/5.3/ [Accessed 11 November 2026].
Docker Inc., 2026. Dockerfile reference [Online]. Available at: https://docs.docker.com/reference/dockerfile/ [Accessed 11 November 2026].
GitHub, 2026. GitHub Actions documentation [Online]. Available at: https://docs.github.com/en/actions [Accessed 15 October 2026].
Google, 2026. Google Fonts: Fraunces and DM Sans [Online]. Available at: https://fonts.google.com/ [Accessed 14 November 2026].
Kalpin, N. and Lander, R., 2023. Pro ASP.NET Core 7. 10th ed. New York: Apress.
Microsoft, 2026a. ASP.NET Core documentation [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/ [Accessed 15 October 2026].
Microsoft, 2026b. Entity Framework Core documentation [Online]. Available at: https://learn.microsoft.com/en-us/ef/core/ [Accessed 15 October 2026].
Microsoft, 2026c. Session and state management in ASP.NET Core [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state [Accessed 15 October 2026].
Microsoft, 2026d. Azure Blob Storage client library for .NET [Online]. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet [Accessed 11 November 2026].
Microsoft, 2026e. Use the Azurite emulator for local Azure Storage development [Online]. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite [Accessed 11 November 2026].
Microsoft, 2026f. Containerize an ASP.NET Core app [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/ [Accessed 11 November 2026].
Moriarty, K., Kalpin, N. and Lander, R., 2022. .NET MAUI and ASP.NET Core: Modern Web Development. Sebastopol, CA: O'Reilly Media.
Mozilla, 2026. HTTP authentication [Online]. Available at: https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication [Accessed 15 October 2026].
OWASP Foundation, 2024. Password Storage Cheat Sheet [Online]. Available at: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html [Accessed 15 October 2026].
RFC Editor, 2000. RFC 2898: PKCS #5: Password-Based Cryptography Specification Version 2.0 [Online]. Available at: https://datatracker.ietf.org/doc/html/rfc2898 [Accessed 15 October 2026].
Swagger, 2026. OpenAPI Specification [Online]. Available at: https://swagger.io/specification/ [Accessed 15 October 2026].
xUnit.net, 2026. xUnit.net documentation [Online]. Available at: https://xunit.net/ [Accessed 15 October 2026].
