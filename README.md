# Vehicle Explorer

Pick a car make and a model year, then see the matching vehicle types and models.
Data comes from the [NHTSA vPIC API](https://vpic.nhtsa.dot.gov/api/).

**Live demo:** _AWS link goes here_

| Layer    | Tech                                                                                      |
| -------- | ----------------------------------------------------------------------------------------- |
| Backend  | .NET 10 ASP.NET Core Web API, onion layers (Domain / Application / Infrastructure / Api) |
| Frontend | Angular 22, a VehicleExplorer component + VehicleService, plain HTML/CSS (no UI library) |
| Tests    | xUnit + `WebApplicationFactory` (backend), Vitest (frontend)                              |
| Delivery | Docker (single image), GitHub Actions → GHCR, AWS EC2 (Free Tier)                         |

Design decisions, trade-offs and how AI was used are in [DECISIONS.md](DECISIONS.md).

---

## Run it locally

### Option A: Docker (one command)

Prerequisite: [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or any Docker engine with Compose).

```bash
git clone https://github.com/mahmoud-ashour96/vehicle-explorer.git
cd vehicle-explorer
docker compose up --build
```

Open **http://localhost:8080**. The same container serves the UI and the API.

Stop it with `Ctrl+C`, then `docker compose down`.

### Option B: Run the backend and frontend separately (for development)

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24](https://nodejs.org/) (`frontend/.nvmrc` pins it; with nvm, run `nvm use` in `frontend/`)

**1. Start the API** (terminal 1):

```bash
dotnet run --project backend/src/VehicleExplorer.Api
```

The API listens on http://localhost:5226. Open http://localhost:5226/api/model-years to check it; it should return a list of years.

**2. Start the Angular dev server** (terminal 2):

```bash
cd frontend
npm ci
npm start
```

Open **http://localhost:4200**. The dev server forwards `/api/*` to the API (see `frontend/proxy.conf.json`), so no CORS setup is needed.

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

| Method | Route                                                  | Description                               |
| ------ | ------------------------------------------------------ | ----------------------------------------- |
| GET    | `/api/makes`                                           | All makes, sorted by name                 |
| GET    | `/api/model-years`                                     | Supported model years, newest first       |
| GET    | `/api/makes/{makeId}/vehicle-types`                    | Vehicle types for a make                  |
| GET    | `/api/makes/{makeId}/models?year={year}`               | Models for a make and year                |
| GET    | `/api/makes/{makeId}/models?year={year}&vehicleType=…` | Same, filtered by a vehicle type name     |

Errors use [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457): `400` for an invalid year, `502` when vPIC is unavailable.
`backend/src/VehicleExplorer.Api/VehicleExplorer.Api.http` has ready-made requests.

---

## Project structure

```
backend/
  src/
    VehicleExplorer.Domain/           Entities (Make, VehicleType, VehicleModel) and the model-year rule
    VehicleExplorer.Application/      DTOs, IVehicleService / VehicleService (validation, caching, mapping),
                                      IVehicleDataSource interface, application exceptions
    VehicleExplorer.Infrastructure/   VpicVehicleDataSource: calls NHTSA vPIC, maps its JSON to entities
    VehicleExplorer.Api/              Controllers, global exception handler, Program.cs
  tests/
    VehicleExplorer.Tests/            Domain, Application, Infrastructure and Api tests
frontend/src/app/
  app.ts / app.html                   Page layout: header + <app-vehicle-explorer>
  vehicle-explorer/                   VehicleExplorer component: make, year and type pickers, and the results
  vehicle.service.ts                  Calls the API with HttpClient
  vehicle.models.ts                   Make, VehicleType, VehicleModel interfaces
deploy/                               EC2 user data and update scripts
Dockerfile                            Multi-stage: Angular build -> .NET publish -> aspnet runtime
```

Dependencies point inwards: `Api → Infrastructure → Application → Domain`.

---

## Deploying to AWS

CI publishes the image to `ghcr.io/<owner>/vehicle-explorer` from `main`. The live demo runs that image on an EC2 `t3.micro` (Free Tier):

1. In GitHub, open **Packages → vehicle-explorer → Package settings** and set the visibility to **Public**, so EC2 can pull the image without credentials.
2. Launch an EC2 instance: Amazon Linux 2023, `t3.micro`, with a security group that allows inbound **HTTP (80)** only. Attach an IAM role with `AmazonSSMManagedInstanceCore`, so you can connect with Session Manager instead of opening SSH.
3. Paste `deploy/ec2-user-data.sh` into **Advanced details → User data**.
4. After a minute or two, open `http://<instance-public-dns>`.

To roll out a new version, connect with Session Manager and run `deploy/update.sh`.
No AWS credentials are stored in this repository.
