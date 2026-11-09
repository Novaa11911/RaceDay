# RaceDay Part 3 — MVC Web Application

This is the initial ASP.NET Core MVC frontend for RaceDay Part 3. It uses Razor Views and Bootstrap and communicates with the existing Part 2 REST API through `RaceDayApiClient`. The MVC project does not access the database directly.

## Requirements
- .NET 8 SDK (local development)
- The RaceDay Part 2 API running and reachable from the MVC app
- Docker Desktop (optional container run)

## Configure the API URL
Edit `appsettings.json` and set `RaceDayApi:BaseUrl` to the API's reachable base URL. For local development, `http://localhost:5080/` matches the Part 2 launch settings in the supplied code. From inside Docker, `localhost` refers to the MVC container, not your host or the API container. Set the value to the API service name on the same Docker network, for example `http://raceday-api:8080/`, or use `host.docker.internal` where appropriate.

## Run locally
From this directory:

```bash
dotnet restore
dotnet run
```

Open the URL printed by `dotnet run`.

## Build and run with Docker
Build the image:

```bash
docker build -t raceday-mvc .
```

Run the MVC container with the API URL configured for your environment:

```bash
docker run --rm -p 8080:8080 -e RaceDayApi__BaseUrl=http://host.docker.internal:5080/ --name raceday-mvc raceday-mvc
```

Then open `http://localhost:8080`. If the API is also in Docker, put both containers on the same Docker network and use the API container/service name instead of `host.docker.internal`.

## Current scope
- Shared responsive RaceDay navigation and visual styling
- Home page and event listing/detail pages reading event data from `GET /api/events`
- Login and registration forms using `POST /api/auth/login` and `POST /api/auth/register`
- API cookie forwarding from the MVC server to the API for subsequent requests
- Graceful API connection/time-out messages
- Dockerfile and container run instructions

## Integration notes
The current Part 2 API's login endpoint issues its own `RaceDay.Auth` cookie. This MVC client forwards the API cookie server-side. Do not expose the API cookie to browser JavaScript. Additional role-specific workflows (enrolment, category/result management, profile changes and image uploads) should be connected to the exact API request contracts and verified against the running API before submission.

## Verification status
This is a starter implementation and has not been compiled or run in this environment. Run `dotnet build` and test against the live Part 2 API before treating it as submission-ready.
