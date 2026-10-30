# Decisions

## Architecture at a glance

```
Browser (Angular 22)
   │  same origin: /api/* and static files
   ▼
VehicleExplorer.Api             MakesController, ModelYearsController, GlobalExceptionHandler
   │
   ▼
VehicleExplorer.Application     VehicleService: validation, caching, entity → DTO mapping
   │  IVehicleDataSource (interface)
   ▼
VehicleExplorer.Infrastructure  VpicVehicleDataSource: typed HttpClient + resilience
   │
   ▼
NHTSA vPIC API

VehicleExplorer.Domain          Entities + model-year rule (referenced by all layers, depends on nothing)
```

## Key decisions and trade-offs

### A light onion architecture
- There are four projects, and dependencies point inwards (`Api → Infrastructure → Application → Domain`).
- **Application** owns the `IVehicleDataSource` interface. **Infrastructure** implements it for vPIC. Swapping the data source, or faking it in tests, touches nothing else.
- I deliberately left out MediatR, CQRS, repositories and AutoMapper. With four read-only endpoints they would add ceremony without adding value. Mapping is a few explicit `ToDto()` extension methods.
- **Trade-off:** domain entities and DTOs currently have the same shape, which looks like duplication. I kept them separate so the API contract can change without touching the domain, and the other way round.

### DTOs as plain classes
API contracts (`MakeDto`, `VehicleTypeDto`, `VehicleModelDto`) are classes with `{ get; set; }` properties. This matches what most .NET teams use for DTOs and serializes the same as records would.

### Business rules live in the Application layer
- `VehicleService` validates input and throws `ValidationException`. The API's `GlobalExceptionHandler` turns that into **400 ValidationProblemDetails**. Controllers stay thin: they take parameters and return results.
- **Model year** must fall between **1981** (when the 17-character VIN standard begins) and **next year** (next year's models are already on sale). The rule lives in `Domain/Rules/ModelYears.cs` and is exposed as `/api/model-years`, so the UI dropdown and the validation can't disagree.
- **Vehicle type** goes into vPIC's URL **path**, so it is checked against an allow-list (letters, digits, spaces, `( ) -`, up to 100 characters) and URL-escaped. Input like `../../x` is rejected.
- vPIC matches vehicle types partially (`car` also matched "Passenger Car"), so the UI sends the exact names returned by the vehicle-types endpoint.

### The backend proxies vPIC instead of the browser calling it
- **Why:** caching, one place for resilience and error handling, and a stable contract (`{ id, name }` instead of vPIC's `Make_ID` / `Model_Name` envelope).
- vPIC quirks (blank names, duplicate rows, HTML error pages) are cleaned up in Infrastructure, so they never reach the domain.

### Caching: `IMemoryCache` with a size limit, plus a startup warm-up
- `getallmakes` returns **12,416 makes (~600 KB) and took ~3.4 s** when I measured it. Makes and vehicle types are cached for 24 h, models for 6 h. A cached models request took ~3 ms; uncached it took ~270 ms.
- `SizeLimit` caps the number of cache entries, so random make/year/type combinations can't grow memory without limit. Failed calls are never cached.
- A `BackgroundService` loads the makes list at startup, so the first visitor doesn't wait 3 s.
- Responses also send `Cache-Control: max-age=3600`, and Brotli/gzip compression cuts the makes payload to ~173 KB.
- **Trade-off:** the cache is per instance. That's fine for one container. For several instances I would use `HybridCache` with Redis.

### Failure handling
- `AddStandardResilienceHandler()` adds retries with backoff, timeouts and a circuit breaker to the vPIC HttpClient.
- Every vPIC failure (network error, non-2xx status, non-JSON body, timeout, open circuit) becomes an `ExternalServiceException`, which the handler returns as **502 ProblemDetails**. Unknown `/api/*` routes return a real 404 instead of the Angular `index.html`.
- `VpicOptions` is bound from configuration and validated at startup, so a missing base URL fails fast instead of failing on the first request.

### Make search runs in the browser, over the full list
- The client downloads the make list once (compressed, and cached by the browser for an hour) and filters it locally. Prefix matches rank first, and the list is capped at 50 suggestions.
- **Why:** search is instant, with no request per keystroke and no debounce or race handling.
- **Trade-off:** a ~173 KB first load. A server-side `?search=` endpoint would be the next step if the list grew much larger.
- With an empty search box, the app suggests **popular makes**, also offered as quick-pick buttons. vPIC's alphabetical list starts with entries like "#1 ALPINE CUSTOMS", which is not a useful starting point.

### Angular: a plain service plus signals, kept simple
- `VehicleService` is an ordinary `HttpClient` service that returns Observables, the pattern most Angular developers know.
- The page component holds the selection as signals and loads data with `rxResource` (stable in Angular 22). When the make or year changes, the matching resource reloads automatically and **cancels the previous request**, so a slow old response can't overwrite a newer one. There is no separate store or state library.
- Angular 22 defaults are used as they come: standalone components, zoneless change detection, OnPush.
- The UI shows loading skeletons, guided empty states ("Find a vehicle", "Now pick a model year", "No models found"), and an error alert with **Try again** that uses the API's `detail` message.

### UI library: Angular Material, not PrimeNG
- I considered PrimeNG. Its recent releases (including 20–22) are now under a commercial **PrimeUI License**, which needs a license key; without one, the app shows a license banner. A key would also have to sit in the frontend code of a public repository.
- Angular Material is MIT-licensed, maintained by the Angular team, and covers what this page needs: an accessible autocomplete, select and chips.
- **Trade-off:** ~123 KB transferred for the initial bundle. I raised the bundle warning budget to 750 KB raw to match.

### One container for UI and API
- The Dockerfile builds Angular, publishes .NET, and copies the Angular output into `wwwroot`. Same origin, so there is no CORS setup, one artifact to deploy, and one health check.
- Hashed assets get `immutable` caching for a year; `index.html` is `no-cache`, so new deployments show up immediately.
- **Trade-off:** the UI can't be scaled or CDN-hosted on its own. At larger scale I would host the static files on S3 + CloudFront.

### Delivery and AWS
- GitHub Actions runs both test suites and builds the image. On `main` it also pushes the image to GHCR using the built-in `GITHUB_TOKEN`, so no secrets are stored.
- An EC2 `t3.micro` (Free Tier) pulls and runs the image. Building on the instance would be risky with only 1 GB of RAM.
- The security group opens **only port 80**, and access to the instance goes through **SSM Session Manager**, so there is no SSH port and no key pair. No credentials are in the repository.

## Assumptions
- Vehicle types belong to a make, not to a make and year, because vPIC's endpoint takes no year. They appear as soon as a make is picked and act as an optional filter on the models.
- Make and model names are shown as vPIC returns them (makes are uppercase). Title-casing would mangle names like "BMW".
- No authentication or persistence is needed. All data is public and read-only.

## How I used AI

I built this with **Claude Code** (Claude Opus) as a pair programmer. It scaffolded the projects, drafted most of the code and tests, and drafted these docs. I set the direction (for example the onion structure, DTO classes and a simpler frontend), reviewed every change, ran the app against the live API, and made the calls on structure and trade-offs.

**What it got wrong, and how it was fixed:**

1. **An Angular test that hung.** The first component test awaited `fixture.whenStable()` *before* flushing the mocked HTTP requests, and timed out after 5 s. In Angular 22, pending resource requests count as pending work, so the app is never "stable" until they are answered. The fix: run change detection, flush the requests, and only then await stability. The specs now have a comment explaining this.
2. **`[ResponseCache(VaryByQueryKeys = …)]` without the middleware.** The AI added `VaryByQueryKeys` to the models endpoint. That property only works with the Response Caching middleware; without it, ASP.NET Core throws at request time. I caught it in review and removed it. A plain `Cache-Control` header is enough, because browsers already cache per URL, including the query string.
3. **A dependency in the wrong package.** When moving caching into the Application layer, the AI referenced `Microsoft.Extensions.Caching.Abstractions` but called `AddMemoryCache()`, which lives in `Microsoft.Extensions.Caching.Memory`. It was fixed before the first build.
4. **Smaller type errors**, which the compiler caught: an HTTP params object built as `{ year, vehicleType: undefined }` doesn't satisfy Angular's params type, and an app initializer returned a value where `void` was expected.

## Next steps
- Keep the selection in the URL query string, so results can be shared and bookmarked.
- Add Playwright end-to-end tests for the main flow.
- Serve over HTTPS on a custom domain (CloudFront or an ALB with ACM), and add rate limiting on `/api`.
- Add OpenTelemetry traces and metrics: cache hit rate and vPIC latency.
- Use `HybridCache` + Redis if the app ever runs on more than one instance.
- Define the AWS setup as code (CDK or Terraform) instead of console steps.
