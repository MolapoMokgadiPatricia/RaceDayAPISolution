## RaceDay
RaceDay is a full-stack event management platform for South African road running, walking and cycling events. It has three parts that build on each other:

| Part | What it is |
|---|---|
| Part 1 | ERD, API endpoint plan and SQL script (in `/docs`) |
| Part 2 | `RaceDayAPI`: RESTful ASP.NET Core 8 Web API with EF Core, session authentication, Swagger and xUnit tests |
| Part 3 | `RaceDayMVC`: ASP.NET Core MVC website that consumes the API, Azure Blob Storage image uploads, and Docker |

## Technology Stack
- ASP.NET Core 8 (Web API and MVC), C# 12
- Microsoft SQL Server with Entity Framework Core 8 (Code-First)
- Session-based authentication with hashed passwords
- Azure Blob Storage (Azurite emulator for development) for event banners and profile pictures
- Bootstrap 5, custom CSS and JavaScript
- Swagger / OpenAPI, xUnit
- Docker, GitHub Actions

## STUDENT
• Author Name: Molapo Mokgadi Patricia

• Student Identification Number: ST10487723

• Module: PROG6212 — Programming 2B

• GitHub: @MolapoMokgadiPatricia

## PROJECT STRUCTURE 
<img width="568" height="285" alt="Screenshot 2026-10-15 151249" src="https://github.com/user-attachments/assets/ef74cb3b-5b23-4c40-98b3-39eddbe26ed2" />


## Roles
| Role | What they can do |
|---|---|
| **Organiser** | Create, edit and delete events (with a banner image), define categories, view and confirm or decline enrolments, capture and publish finish times and positions |
| **Participant** | Browse and filter events, enter an event by choosing a category, see their entries and statuses, see their race history, edit their profile and upload a profile picture |

Roles are enforced in the API (`[AuthorizeRole]`) and again in the MVC site (`[SessionAuthorize]`), and each role has its own navigation menu.

## Part 3: MVC Web Application

### Pages
| Area | Pages |
|---|---|
| Everyone | Home (hero and live-filtered upcoming events), Events, Event details, Login, Register |
| Participant | Event details with **Enter event** and category choice, My events (colour-coded status, cancel entry), My results (event, date, category, finish time and position such as "47th out of 312 finishers"), Profile with picture upload |
| Organiser | Dashboard (events, enrolments per event, upcoming dates), My events (create, edit, delete, banner upload), Categories, Enrolments (confirm or decline), Results entry (with "Rank by finish time") |

Enrolment colours: **Confirmed** (API value `Approved`) green, **Pending** amber, **Rejected** red, **Cancelled** grey.

### How the MVC site talks to the API
- `RaceDayMVC` has no database access and no business logic. Every action goes through `Services/RaceDayApiClient.cs`.
- On login the API's session cookie is kept in the MVC server-side session together with the user id and role, and is forwarded on every API call.
- Logging out clears both sessions. Protected pages redirect visitors who are not logged in to the login page, and show an access-denied page for the wrong role.
- Files are never sent to Azure by the MVC site. Uploads go to the API, which stores them in Blob Storage and saves the URL in the database.

### API endpoints added in Part 3
| Method | Route | Role | Purpose |
|---|---|---|---|
| POST | `/api/EventBanners/{id}/banner` | Organiser (own event) | Upload an event banner to Blob Storage and save its URL |
| POST | `/api/ProfilePictures` | Participant | Upload a profile picture to Blob Storage and save its URL |
| GET | `/api/results/my-history` | Participant | Race history with finish time, position and number of finishers |
| GET / PUT | `/api/users/{id}` | Any (own profile) | Now includes date of birth, emergency contact and profile picture URL |

`PUT /api/events/{id}` now also saves the event description.

### Run locally
1. Start Azurite (Visual Studio's built-in Azurite, or `docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0`). `appsettings.json` already points the API at it with `UseDevelopmentStorage=true`.
2. Run `RaceDayAPI` (the `https` profile gives https://localhost:7143). Swagger is at `/swagger`.
3. Run `RaceDayMVC` (the `https` profile gives https://localhost:7200). If the API runs elsewhere, change `ApiSettings:BaseUrl` in `RaceDayMVC/appsettings.json`.
4. Register an Organiser and a Participant on the Register page, or use the seed data from `docs/RaceDayDatabase.sql`.

### Azure Blob Storage
- Containers `event-banners` and `profile-pictures` are created automatically with public read access for blobs.
- To use a real storage account, set the connection string on the API without committing it:
```
cd RaceDayAPI
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AzureBlobStorage" "<your connection string>"
```
and enable "Allow Blob anonymous access" on the storage account.
- Images must be JPEG, PNG or WebP, up to 5 MB. The API checks size, type, extension and the real file signature.

### Docker
The MVC site runs in a container. With the API running on your machine on port 5108 (use the `http` launch profile), run:

```
docker build -t raceday-mvc ./RaceDayMVC
docker run --rm -p 5200:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5108 raceday-mvc
```

Then open http://localhost:5200. On Linux add `--add-host=host.docker.internal:host-gateway` to the run command.

The Dockerfile uses a multi-stage build (SDK image to build, ASP.NET runtime image to run), restores packages in their own cached layer, and runs as the non-root user.

Optional: `docker compose up --build` starts SQL Server, the API and the website together. Set `SA_PASSWORD` and `AZURE_STORAGE_CONNECTION_STRING` first.

### Unit tests
`dotnet test` runs the xUnit tests for authentication, events, categories, enrolments, results, password hashing and the new image upload endpoints (ownership, validation of real image content, missing files, missing participant).

## GITHUB ACTION SCREENSHOT 
<img width="655" height="161" alt="image" src="https://github.com/user-attachments/assets/f3ea904d-003f-4c70-9f27-2e41759b90ed" />

## YOUTUBE LINK 
(paste your unlisted YouTube link here)

## AI TOOL USED
Claude (Anthropic): used for structural syntax refinement, environment review cycles, logic debugging assistance and verification alignment in Part 2. In Part 3 it was also used to help build the MVC views, stylesheet and JavaScript, the image upload endpoints, the Dockerfile and the CI workflow.

## REFERENCES

• Anthropic, 2026. Claude AI assistant [Online]. Available at: https://claude.ai [Accessed 15 October 2026].

• GitHub, 2026. GitHub Actions documentation [Online]. Available at: https://docs.github.com/en/actions [Accessed 15 October 2026].

• Kalpin, N. and Lander, R., 2023. Pro ASP.NET Core 7. 10th ed. New York: Apress.

• Microsoft, 2026a. ASP.NET Core documentation [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/ [Accessed 15 October 2026].

• Microsoft, 2026b. Entity Framework Core documentation [Online]. Available at: https://learn.microsoft.com/en-us/ef/core/ [Accessed 15 October 2026].

• Microsoft, 2026c. Session and state management in ASP.NET Core [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state [Accessed 15 October 2026].

• Moriarty, K., Kalpin, N. and Lander, R., 2022. .NET MAUI and ASP.NET Core: Modern Web Development. Sebastopol, CA: O'Reilly Media.

• Mozilla, 2026. HTTP authentication [Online]. Available at: https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication [Accessed 15 October 2026].

• OWASP Foundation, 2024. Password Storage Cheat Sheet [Online]. Available at: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html [Accessed 15 October 2026].

• RFC Editor, 2000. RFC 2898: PKCS #5: Password-Based Cryptography Specification Version 2.0 [Online]. Available at: https://datatracker.ietf.org/doc/html/rfc2898 [Accessed 15 October 2026].

• Swagger, 2026. OpenAPI Specification [Online]. Available at: https://swagger.io/specification/ [Accessed 15 October 2026].

• xUnit.net, 2026. xUnit.net documentation [Online]. Available at: https://xunit.net/ [Accessed 15 October 2026].

• Docker Inc., 2026. Dockerfile reference [Online]. Available at: https://docs.docker.com/reference/dockerfile/ [Accessed 11 November 2026].

• Microsoft, 2026d. Azure Blob Storage client library for .NET [Online]. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet [Accessed 11 November 2026].

• Microsoft, 2026e. Use the Azurite emulator for local Azure Storage development [Online]. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite [Accessed 11 November 2026].

• Microsoft, 2026f. Containerize an ASP.NET Core app [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/ [Accessed 11 November 2026].

• Bootstrap, 2026. Bootstrap 5.3 documentation [Online]. Available at: https://getbootstrap.com/docs/5.3/ [Accessed 11 November 2026].
