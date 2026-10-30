# Vehicle Explorer

Pick a car make and a model year, then see the matching vehicle types and models.
Data comes from the [NHTSA vPIC API](https://vpic.nhtsa.dot.gov/api/).

**Live demo:** _AWS link goes here_

| Layer    | Tech                                                                         |
| -------- | ---------------------------------------------------------------------------- |
| Backend  | .NET 10 ASP.NET Core Web API (controllers), typed `HttpClient` + resilience, in-memory cache |
| Frontend | Angular 22 (standalone, zoneless, signals, `httpResource`), Angular Material |
| Tests    | xUnit + `WebApplicationFactory` (API), Vitest (Angular)                      |
| Delivery | Docker (single image), GitHub Actions → GHCR, AWS EC2 (Free Tier)            |

Design decisions, trade-offs and how AI was used are in [DECISIONS.md](DECISIONS.md).

---

## Run it locally

### Option A: Docker (one command)

Prerequisite: [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or any Docker engine with Compose).

```bash
git clone https://github.com/<your-username>/vehicle-explorer.git
cd vehicle-explorer
docker compose up --build
```

Open **http://localhost:8080**. The same container serves the UI and the API.

Stop it with `Ctrl+C`, then `docker compose down`.

### Option B: Run the backend and frontend separately (for development)

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24](https://nodejs.org/) (`frontend/.nvmrc` pins it; with nvm run `nvm use` in `frontend/`)

**1. Start the API** (terminal 1):

```bash
dotnet run --project backend/VehicleExplorer.Api
```

The API listens on http://localhost:5226. Check it with http://localhost:5226/health. It should say `Healthy`.

**2. Start the Angular dev server** (terminal 2):

```bash
cd frontend
npm ci
npm start
```

Open **http://localhost:4200**. The dev server proxies `/api/*` to the API (see `frontend/proxy.conf.json`), so no CORS setup is needed.

---

## Run the tests

```bash
# Backend: unit + integration tests (vPIC is faked, no network needed)
dotnet test backend/VehicleExplorer.slnx

# Frontend: Vitest
cd frontend
npx ng test --watch=false
```

CI runs both suites and the Docker build on every push and pull request (`.github/workflows/ci.yml`).

---

## API

| Method | Route                                                  | Description                                  |
| ------ | ------------------------------------------------------ | -------------------------------------------- |
| GET    | `/api/makes`                                           | All makes, sorted by name                    |
| GET    | `/api/model-years`                                     | Model years the API accepts, newest first    |
| GET    | `/api/makes/{makeId}/vehicle-types`                    | Vehicle types for a make                     |
| GET    | `/api/makes/{makeId}/models?year={year}`               | Models for a make and year                   |
| GET    | `/api/makes/{makeId}/models?year={year}&vehicleType=…` | Same, filtered by a vehicle type name        |
| GET    | `/health`                                              | Liveness check                               |

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457): `400` for invalid input, `502` when vPIC is unavailable.
`backend/VehicleExplorer.Api/VehicleExplorer.Api.http` has ready-made requests. In Development, the OpenAPI document is at `/openapi/v1.json`.

---

## Project structure

```
backend/
  VehicleExplorer.Api/
    Controllers/        HTTP endpoints and input validation
    Services/           VehicleCatalog (caching) and startup cache warm-up
    Vpic/               Typed client for NHTSA vPIC, its DTOs and error type
    Models/             Public API contracts and the model-year rule
    Infrastructure/     Maps upstream failures to 502 ProblemDetails
  VehicleExplorer.Api.Tests/
frontend/
  src/app/
    vehicles/           Page component, signal store, make autocomplete
    shared/             Error banner
deploy/                 EC2 user-data and update scripts
Dockerfile              Multi-stage: Angular build -> .NET publish -> aspnet runtime
```

---

## Deploying to AWS

CI publishes the image to `ghcr.io/<owner>/vehicle-explorer` from `main`. The live demo runs that image on an EC2 `t3.micro` (Free Tier):

1. In GitHub, open the repository's **Packages → vehicle-explorer → Package settings** and set the visibility to **Public**, so EC2 can pull the image without credentials.
2. Launch an EC2 instance: Amazon Linux 2023, `t3.micro`, a security group that allows inbound **HTTP (80)** only. Attach an IAM role with `AmazonSSMManagedInstanceCore`, so you can connect with Session Manager instead of opening SSH.
3. Paste `deploy/ec2-user-data.sh` into **Advanced details → User data**, with `OWNER` replaced by your GitHub username.
4. After a minute or two, open `http://<instance-public-dns>`.

To roll out a new version, connect with Session Manager and run `deploy/update.sh`.
No AWS credentials are stored in this repository.
