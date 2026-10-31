# Decisions

## Architecture at a glance

```
Browser (Angular 22)
   │  same origin: /api/* and static files
   ▼
VehicleExplorer.Api             MakesController, ModelYearsController, GlobalExceptionHandler
   │
   ▼
VehicleExplorer.Application     VehicleService: year validation, caching, entity → DTO mapping
   │  IVehicleDataSource (interface)
   ▼
VehicleExplorer.Infrastructure  VpicVehicleDataSource: HttpClient calls to vPIC
   │
   ▼
NHTSA vPIC API

VehicleExplorer.Domain          Entities + model-year rule (used by all layers, depends on nothing)
```

## Key decisions and trade-offs

### Simple first
The brief asked for a small app, and I expect to change it live in the interview, so I kept only what the requirements need. Every class has one obvious job, and adding a feature means touching one place per layer: an endpoint in a controller, a method on `VehicleService`, and a vPIC call in `VpicVehicleDataSource`.

I deliberately left out things a production service would add, and listed them under [Next steps](#next-steps): retries and a circuit breaker, a startup cache warm-up, response compression, HTTP caching headers, health checks and OpenAPI.

### A light onion architecture
- Four projects, with dependencies pointing inwards: `Api → Infrastructure → Application → Domain`.
- **Application** owns the `IVehicleDataSource` interface, and **Infrastructure** implements it for vPIC. Replacing the data source, or faking it in tests, touches nothing else.
- No MediatR, CQRS, repositories or AutoMapper. With four read-only endpoints they would add ceremony without value. Mapping is a few explicit `ToDto()` extension methods.
- **Trade-off:** domain entities and DTOs currently have the same shape. I kept them separate so the API contract and the domain can change independently.

### DTOs as plain classes
API contracts (`MakeDto`, `VehicleTypeDto`, `VehicleModelDto`) are classes with `{ get; set; }` properties, the way most .NET teams write DTOs.

### Validation and errors
- **Model year** must be between **1981** (when the 17-character VIN standard begins) and **next year** (next year's models are already on sale). The rule lives in `Domain/Rules/ModelYears.cs`, and `/api/model-years` exposes it, so the UI dropdown and the validation always agree.
- `VehicleService` throws `ValidationException` for an invalid year. `GlobalExceptionHandler` maps it to **400**, and maps vPIC failures (`ExternalServiceException`) to **502**. Controllers contain no error handling of their own.
- The vehicle type is URL-escaped before it goes into vPIC's URL path. vPIC matches types partially (`car` also matched "Passenger Car"), so the UI sends the exact names returned by the vehicle-types endpoint.

### The backend calls vPIC, not the browser
- **Why:** one place for caching and error handling, and a clean contract (`{ id, name }` instead of vPIC's `Make_ID` / `Model_Name` envelope).
- I checked the real data and clean up only what actually occurs: about 80 make names have extra spaces (they are trimmed), and some model lists repeat the same model (they are de-duplicated). For example, Ford 2020 returned 34 rows for 25 models.

### Caching: one `IMemoryCache`, 6 hours
- This is the one extra beyond the brief, kept for a reason: `getallmakes` returns **12,416 makes (~600 KB)** and took **2–3 s**, once **44 s**, when I measured it. With the cache, the repeat request took ~30 ms.
- `GetOrCreateAsync` with one duration for everything. Failed calls are not cached, because an exception means nothing is stored.
- **Trade-off:** the first request after startup, or after expiry, still waits for vPIC. The cache is also per instance, which is fine for one container.

### Make search runs in the browser
- The client downloads the make list once and filters it locally. Prefix matches rank first, and the list is capped at 50 suggestions. Search is instant, with no request per keystroke.
- With an empty search box the app suggests **popular makes**. vPIC's alphabetical list starts with entries like "#1 ALPINE CUSTOMS", which is not a useful starting point.

### Angular: a plain service plus signals
- `VehicleService` is an ordinary `HttpClient` service that returns Observables.
- The page component keeps the selection in signals and loads data with `rxResource` (stable in Angular 22). When the make or year changes, the data reloads and the previous request is **cancelled**, so a slow old response can't overwrite a newer one. There is no separate store.
- The UI shows loading skeletons, guided empty states, and an error alert with **Try again**.

### UI library: Angular Material, not PrimeNG
- Recent PrimeNG releases (including 20–22) are under a commercial **PrimeUI License** that requires a license key. Without a key the app shows a license banner, and a key would have to sit in the frontend code of a public repository.
- Angular Material is MIT-licensed, maintained by the Angular team, and has the accessible autocomplete, select and chips this page needs.

### One container for UI and API
- The Dockerfile builds Angular, publishes .NET, and copies the Angular output into `wwwroot`. Same origin means no CORS setup, and there is one artifact to deploy.
- **Trade-off:** the UI can't be scaled or served from a CDN on its own.

### Delivery and AWS
- GitHub Actions runs both test suites and builds the image. On `main` it pushes the image to GHCR using the built-in `GITHUB_TOKEN`, so no secrets are stored.
- An EC2 `t3.micro` (Free Tier) pulls and runs the image. Building on the instance would be risky with 1 GB of RAM.
- The security group opens **only port 80**. Access to the instance goes through **SSM Session Manager**, so there is no SSH port and no key pair. No credentials are in the repository.

## Assumptions
- Vehicle types belong to a make, not to a make and year, because vPIC's endpoint takes no year. They appear as soon as a make is chosen and act as an optional filter on the models.
- Names are shown as vPIC returns them (makes are uppercase). Title-casing would mangle names like "BMW".
- No authentication or persistence is needed. All data is public and read-only.

## How I used AI

I built this with **Claude Code** (Claude Opus) as a pair programmer. It scaffolded the projects, drafted most of the code and tests, and drafted these docs. I set the direction, reviewed every change, ran the app against the live API, and made the structural calls. One of those calls was cutting the AI's first version down: it had added retries, cache warm-up, compression and other production extras that the brief didn't ask for and that made the code harder to change.

**What it got wrong, and how it was fixed:**

1. **An Angular test that hung.** The first component test awaited `fixture.whenStable()` *before* flushing the mocked HTTP requests, and timed out after 5 s. In Angular 22, pending resource requests count as pending work, so the app is never "stable" until they are answered. The fix: run change detection, flush the requests, then await stability. The specs have a comment explaining why.
2. **`[ResponseCache(VaryByQueryKeys = …)]` without the middleware.** The AI used `VaryByQueryKeys`, which only works with the Response Caching middleware; without it, ASP.NET Core throws at request time. I caught it in review before it ever ran.
3. **A dependency in the wrong package.** The AI referenced `Microsoft.Extensions.Caching.Abstractions` but called `AddMemoryCache()`, which lives in `Microsoft.Extensions.Caching.Memory`. It was fixed before the first build.

## Next steps
- **Resilience:** retries with backoff, a timeout and a circuit breaker on the vPIC `HttpClient` (`AddStandardResilienceHandler`).
- **Faster first load:** warm the makes cache at startup, add response compression, and send `Cache-Control` headers.
- **Operations:** a `/health` endpoint, OpenAPI docs, OpenTelemetry traces, and HTTPS on a custom domain.
- **UX:** keep the selection in the URL so results can be shared; add Playwright end-to-end tests.
- **Scale:** `HybridCache` + Redis if the app runs on more than one instance; AWS setup as code (CDK or Terraform).
