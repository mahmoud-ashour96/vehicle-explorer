# Decisions

## Architecture at a glance

```
Browser (Angular 22)
   │  same origin: /api/*  and static files
   ▼
ASP.NET Core (.NET 10), one container
   ├─ VehiclesController     validation, HTTP contract
   ├─ VehicleCatalog         in-memory cache (IMemoryCache)
   └─ VpicClient             typed HttpClient + standard resilience handler
          │
          ▼
   NHTSA vPIC API
```

The backend is a thin backend-for-frontend over vPIC. It does not copy vPIC 1:1.

## Key decisions and trade-offs

### Backend proxies vPIC instead of the browser calling it directly
- **Why:** caching, one place for resilience and error handling, and a stable contract. The frontend sees `{ id, name }`, not vPIC's `Make_ID`/`Model_Name` envelope. vPIC quirks (blank names, duplicate rows, HTML error pages) are handled server-side.
- **Trade-off:** one extra hop. This is negligible next to vPIC's own latency, which the cache hides anyway.

### Caching: `IMemoryCache` with a size cap, plus a startup warm-up
- `getallmakes` returns **12,416 makes (~600 KB) and took ~3.4 s** when measured. Makes and vehicle types are cached for 24 h and models for 6 h. A cached models call took ~3 ms against ~270 ms uncached.
- `SizeLimit` caps the number of cached lookups, so arbitrary make/year/type combinations can't grow memory without bound.
- A `BackgroundService` loads the makes list at startup, so the first visitor doesn't pay the 3 s.
- Responses also send `Cache-Control: max-age=3600`, and Brotli/gzip compression cuts the makes payload to ~173 KB.
- **Trade-off:** the cache is per instance and has no stampede protection. For one instance with a warm-up, that is acceptable. With several instances, I would move to `HybridCache` backed by Redis.

### Make search runs on the client, over the full list
- The client downloads the make list once, compressed and cached by the browser for an hour, then filters it locally. Prefix matches rank first, and suggestions are capped at 50 with a "keep typing" hint.
- **Why:** search is instant, with no request per keystroke and no debounce/race handling.
- **Trade-off:** a ~173 KB first load. A server-side `?search=` endpoint would be the next step if the list grew much larger.
- With an empty query, the box suggests **popular makes**. vPIC's alphabetical list starts with entries like "#1 ALPINE CUSTOMS", which is not a useful starting point.

### Validation before anything reaches vPIC
- `year` is required and must fall between **1981** (when the 17-character VIN standard begins) and **next year** (next year's models are already on sale). The API exposes this rule as `/api/model-years`, so the dropdown and the validation can't disagree.
- `vehicleType` goes into vPIC's URL **path**. It is checked against an allow-list (`letters, digits, spaces, ( ) -`, max 100 characters) and URL-escaped, so input like `../../x` is rejected.
- vPIC matches vehicle types partially: `car` also matched "Passenger Car". The UI therefore sends the exact names from the vehicle-types endpoint.

### Failure handling
- `AddStandardResilienceHandler()` adds retries with backoff, per-attempt and total timeouts, and a circuit breaker.
- Every upstream failure (network error, non-2xx status, non-JSON body, timeout, open circuit) becomes one `UpstreamServiceException`, which a global handler turns into **502 ProblemDetails**. Invalid input returns **400 ValidationProblemDetails**. Unknown `/api/*` routes return a real 404 instead of the SPA's `index.html`.
- The UI shows each section's loading, empty and error states separately, with **Try again**. Messages use the API's `detail` field.

### Controllers instead of minimal APIs
Both are ASP.NET Core Web API. With controllers, `[ApiController]` gives automatic 400 responses for binding and attribute validation, and the shape is familiar to most .NET teams. The endpoint surface is small enough that the choice is mostly about readability.

### Angular: signals and `httpResource`, no RxJS in app code
- Angular 22 defaults: standalone components, zoneless change detection, OnPush.
- `VehicleExplorerStore` holds the selection as signals. Each data set is an `httpResource` whose request is derived from those signals. Changing the make or year **refetches automatically and cancels the stale request**, which removes the classic race where a slow earlier response overwrites a newer one.
- The store is provided per page rather than in root, so its state is created and destroyed with the page.
- Angular Material supplies an accessible combobox, select and chips. **Trade-off:** ~125 KB transferred for the initial bundle. I raised the bundle budget warning to 750 KB raw to match.

### One container for UI and API
- The Dockerfile builds Angular, publishes .NET, and copies the Angular output into `wwwroot`. The same origin means no CORS, one artifact to deploy, and one health check.
- Hashed assets are served with `immutable` caching for a year. `index.html` is `no-cache`, so new deployments show up immediately.
- **Trade-off:** the UI can't be scaled or CDN-hosted separately. At larger scale I would put the static files on S3 + CloudFront.

### Delivery and AWS
- GitHub Actions runs both test suites, then builds the image. On `main` it pushes the image to GHCR using the built-in `GITHUB_TOKEN`, so no secrets are stored.
- An EC2 `t3.micro` (Free Tier) pulls and runs the image. Building on the instance would be risky with 1 GB of RAM.
- The security group opens **only port 80**. Access to the instance is through **SSM Session Manager**, so there is no SSH port and no key pair. No credentials are in the repository.

## Assumptions
- Vehicle types belong to a make, not a make and year, because vPIC's endpoint takes no year. They therefore appear as soon as a make is picked, and act as an optional filter on the models.
- Make and model names are shown as vPIC returns them (makes are uppercase). Title-casing would mangle names like "BMW".
- No authentication or persistence is needed. All data is public and read-only.

## How I used AI

I built this with **Claude Code** (Claude Opus) as a pair programmer. It scaffolded the projects, drafted most of the code and tests, and drafted these docs. I set the direction, reviewed every change, ran the app against the live API, and made the calls on structure and trade-offs.

**What it got wrong, and how it was fixed:**

1. **Angular test that hung.** In the first version of `app.spec.ts`, the AI awaited `fixture.whenStable()` *before* flushing the mocked HTTP requests. The test timed out after 5 s. The cause: in Angular 22, pending `httpResource` requests count as pending work, so the app is never "stable" until those requests are answered. The fix was to call `fixture.detectChanges()`, flush the requests, and only then await stability. A comment in the spec explains why.
2. **`[ResponseCache(VaryByQueryKeys = …)]` without the middleware.** The AI added `VaryByQueryKeys` to the models endpoint. That property only works with the Response Caching middleware; without it, ASP.NET Core throws at request time. It was caught in review and removed. A plain `Cache-Control` header is enough here, because browsers already cache per URL including the query string.
3. **Smaller type errors:** an `httpResource` params object built as `{ year, vehicleType: undefined }` doesn't satisfy Angular's params type, and an app initializer returned a value where `void` was expected. The compiler caught both and they were fixed straight away.

## Next steps
- Keep the selection in the URL query string, so results can be shared and bookmarked.
- Add Playwright end-to-end tests for the main flow.
- Serve over HTTPS on a custom domain (CloudFront or an ALB with ACM), and add rate limiting on `/api`.
- Add OpenTelemetry traces and metrics, including cache hit rate and vPIC latency.
- Use `HybridCache` + Redis if the app ever runs on more than one instance.
- Define the AWS setup as code (CDK or Terraform) instead of console steps.
