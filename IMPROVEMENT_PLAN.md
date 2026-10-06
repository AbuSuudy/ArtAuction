# ArtAuction Improvement Plan

A review of the full solution: API, Consumer, Contract, Bicep and GitHub workflows. Items are grouped by priority, with file references.

## Suggested Order

1. **Phase 1**: small changes, but several of these are breaking deploys or data right now.
2. **Phase 2 + the immutable-message work from Phase 4**: correctness under retries and concurrent bids.
3. **Phases 5 + 6**: tests and a PR pipeline, so the later refactors are safe.
4. **Phases 3, 4, 7**: API shape, cleanup and hardening.

---

## Phase 1: Bugs That Are Breaking Things Now

- [x] **The code points at different Azure resources than the Bicep creates.** ~Fixed: both addresses now come from config (`ServiceBus:Host`, `StorageAccount:TableUri`) and the configured values match the Bicep-created resource names.
  - Where: `ArtAuction.API/Program.cs:34`, `ArtAuction.Consumer/Program.cs:16,36`
  - Fix: move both addresses into config (`ServiceBus:Namespace`, `Storage:TableUri`) and have Bicep set them as container env vars from module outputs.

- [x] **Every infra deploy resets the apps to the placeholder image.** ~Fixed: `containerApps.bicep` takes an `imageOverrides` param, and `infra.yml` resolves the currently-running image before deploying and passes it back in.
  - Where: `Infra/Modules/containerApps.bicep:33`
  - Fix: add an `image` parameter per app (default to the quickstart image only on first deploy), or look up the current image and pass it back in.

- [x] **The infra workflow passes an empty `--resource-group`**, ~Fixed: now uses `${{ vars.RESOURCE_GROUP }}`.
  - Where: `.github/workflows/infra.yml:59`
  - Fix: use `${{ vars.RESOURCE_GROUP }}`, or better, set the registry identity in Bicep and delete this job.

- [x] **Bids lose their pence when saved.** ~Fixed: all four sites now cast as `(long)(Bid * 100)`.
  - Where: `ArtAuction.Consumer/ArtWorkBidConsumer.cs:20` and all three activities' `Compensate` methods
  - Fix: `(long)(Bid * 100)`, or just store `decimal`/`double`.

- [ ] **The error handler never catches endpoint errors.** ~Mostly fixed: `UseExceptionHandler` is now registered right after `app.Build()`, before `MapBidEndpoints`; `LogError` arguments are in the right order (`logger.LogError(exception, exception?.Message)`); and `Detail = exception?.Message` (line 71) is now null-safe. Still open: the computed `dev` flag is never used to gate what's returned, so exception text still goes to callers in every environment, including Production.
  - Where: `ArtAuction.API/Program.cs:52-75`
  - Fix: only include `Detail` when `dev` is true.

- [ ] **The worker's revision cleanup does nothing.** Still picks revisions that are already inactive (`select(.properties.active == false)`) and then deactivates them — a no-op.
  - Where: `.github/workflows/worker-container.yml:97`
  - Fix: filter on `active == true` and skip the latest revision. With `Single` mode the platform already does this, so the step can probably go.

- [x] **Role assignments are applied to the whole resource group, not the resource.** ~Fixed: `roleAssignments.bicep` is gone; `serviceBus.bicep` and `storageAccount.bicep` now use the AVM modules' resource-scoped `roleAssignments` param.
  - Where: `Infra/Modules/roleAssignments.bicep:5`
  - Fix: use an `existing` resource reference as the scope, or the AVM modules' built-in `roleAssignments` parameter.

- [x] **Accepted and Rejected activities return the wrong log type.** ~Fixed: both now correctly call `context.Completed<BidAcceptedLog>` / `context.Completed<BidRejectedLog>`.
  - Where: `ArtAuction.Consumer/Activities/BidAcceptedActivity.cs:54`, `ArtAuction.Consumer/Activities/BidRejectedActivity.cs:53`
  - Fix: return `BidAcceptedLog` / `BidRejectedLog`.

- [x] **The solution file has the wrong case for `Resources.bicep`.** ~Fixed: `ArtAuction.slnx` now references `Infra/resources.bicep` (lowercase), matching the file on disk.
  - Where: `ArtAuction.slnx:4`
  - Fix: correct the casing.

---

## Phase 2: Message Handling and Domain Correctness

- [ ] **Duplicate messages fail permanently.** `AddEntityAsync` returns 409 if the same message arrives again, so a retry fails and ends up dead-lettered. The compensation inserts have the same problem.
  - Fix: use `UpsertEntityAsync`, or catch 409 and treat it as success.
- [ ] **There's no retry or error policy.** Configure `UseMessageRetry` / `UseDelayedRedelivery` and decide what happens to dead-lettered messages (alerting, a reprocessing path).
- [ ] **Two bids on the same artwork can both be accepted and acquired.** Nothing checks for this. Options:
  - an `Artwork` table entity with ETag optimistic concurrency, or
  - a MassTransit saga state machine per artwork (Open → Sold), which fits the existing routing-slip work.
- [ ] **`BidStaus.Failed` is never set.** Subscribe to routing-slip `Faulted` / `Completed` events so a bid's status reflects the real outcome.
- [ ] **Two of the compensation handlers can never run.** Compensation only runs when a *later* activity faults, and `Aquired` and `BidRejected` are always last. Remove them or document why they're there.
- [ ] **The compensation PartitionKey is the artwork name.** Names can contain `/ \ # ?`, which Table Storage rejects. Use `Art.Id`.
- [ ] **The consumer creates tables inside the DI factory.** These are blocking network calls (`CreateTableIfNotExists` in `ArtAuction.Consumer/Program.cs`). Move them to a startup `IHostedService`, or create the tables in Bicep.
- [ ] **Delete the empty `Worker.cs`** and its `AddHostedService<Worker>()` registration.

---

## Phase 3: API Design

- [ ] **Take the bid as a JSON body, not query parameters on a POST.** Use something like `SubmitBidRequest { ArtworkId, Amount }`, validate it (amount > 0, using .NET 10 minimal-API validation), and use REST-style routes: `POST /api/bids`.
- [ ] **Let callers check a bid's outcome.** Add `GET /api/bids/{id}` that reads `BidStatus`, and return `202 Accepted` with that URL as the `Location`. Right now a client can't find out what happened.
- [ ] **Move the artwork catalog out of the static list** (`ArtAuction.API/Endpoints/BidEndpoints.cs:9`) into Table Storage or config, and add `GET /api/artworks`.
- [ ] **Remove dead code:**
  - the commented-out lambda endpoint in `BidEndpoints.cs`
  - the stale `weatherforecast` request in `ArtAuction.API.http`
  - the commented-out `IsDevelopment()` check in `Program.cs`
- [ ] **Gate Scalar/OpenAPI behind config** instead of exposing it unconditionally in production.
- [ ] **Add health checks** (`/health/live`, `/health/ready` covering Service Bus and Storage) and wire them to Container Apps liveness/readiness probes.
- [ ] **`UseHttpsRedirection` does nothing behind Container Apps ingress**, which handles TLS. Make it conditional or remove it.

---

## Phase 4: Code Structure and Quality

- [ ] **Keep storage types out of the shared contract.** `ArtAuction.Contract` references `Azure.Data.Tables`, so the API pulls in a storage SDK it never uses. Keep only message types there. Move `BidEntity` / `BidCompensationEntity` into the Consumer, or a separate `ArtAuction.Persistence` project.
- [ ] **Make messages immutable** (records with `init` / `required`). This also fixes the nullable warnings: `ArtWorkBid.Art` and the activity argument/log classes are non-nullable but never initialised.
- [ ] **Fix the typos before they spread further.** Renaming the enum also changes stored values and queue names (e.g. `aquired_execute`), so do them all in one go:
  - Files: `MessaeContact.cs`, `TableEntites.cs`
  - Types/members: `BidStaus`, `Submited`, `Aquired*`, `BidCompensationStaus`, `ArtCollecetion`
  - Log text: `Compensared`
  - Bicep: `containerRegsistryResourceId`, `identites`, `Resouce`
  - `apiAppIndex` in `Infra/resources.bicep:90` actually finds the *consumer*; rename it to match.
- [ ] **Collapse the three near-identical activities** into a shared base class, or one generic status-update helper.
- [ ] **Replace `Console.WriteLine` with `ILogger`** and structured logging, so logs reach App Insights alongside your traces.
- [ ] **Add `Directory.Build.props`** (`TreatWarningsAsErrors`, analyzers, shared `TargetFramework`/`Nullable`/`ImplicitUsings`).
- [ ] **Add `Directory.Packages.props`** for central package versions (MassTransit is versioned twice today).
- [ ] **Use `appsettings` + user-secrets / env vars for every environment-specific value.** Tenant and client IDs are fine to commit, but the Service Bus and storage addresses are not config-driven today.

---

## Phase 5: Testing and Local Development

- [ ] **Add a test project:**
  - `MassTransit.Testing` (`ITestHarness`) for the consumer and activities
  - `WebApplicationFactory` for the API endpoints
- [ ] **Make the stack runnable locally.** Consider **.NET Aspire** (AppHost with the Service Bus emulator + Azurite), or at least a `docker-compose` file. Right now running locally requires real Azure resources and your own credentials.
- [ ] **Configure OpenTelemetry the same way in both services.** The API uses the Azure Monitor distro (`UseAzureMonitor`). The consumer only uses the exporter (`UseAzureMonitorExporter`), so it misses Azure SDK, HTTP and log telemetry. A shared `ServiceDefaults` project (the Aspire pattern) solves this.

---

## Phase 6: CI/CD

- [ ] **Switch to OIDC federated credentials** (`azure/login@v2` + `permissions: id-token: write`) instead of the `AZURE_CREDENTIALS` client-secret JSON and ACR admin username/password.
  - Then disable the ACR admin user (`Infra/Modules/containerRegistry.bicep:9`) and push using `az acr login`.
- [ ] **Fix the script-injection risk.** `${{ github.event.inputs.revision_name }}` is placed directly into a shell script (`.github/workflows/api-container.yml:176`, and likewise in `worker-container.yml`). Pass inputs through `env:` and reference them as `"$REVISION_NAME"`.
- [ ] **Add a PR workflow:** `dotnet build`, `dotnet test`, `az bicep lint`, and a `what-if` on the deployment stack. Today nothing builds or tests before deploying.
- [ ] **Bump actions** (`actions/checkout@v4`, `azure/login@v2`).
- [ ] **Merge the two near-identical container workflows** into one reusable workflow, and drop the single-entry matrices.
- [ ] **Fix the region mismatch.** The deployment stack location is `uksouth` (`infra.yml`) but the RG is `NorthEurope` (`Infra/main.bicep`). Make location a parameter.
- [ ] **Add a smoke test** against the new (green) revision's label URL before anyone switches traffic.
- [ ] **Rollback for the consumer:** in `Single` mode, activating an old revision isn't a clean rollback. Redeploy the old image tag instead.

---

## Phase 7: Infrastructure Hardening and Cost

- [ ] **Storage settings** (`Infra/Modules/storageAccount.bicep`):
  - turn off `allowBlobPublicAccess` (line 13)
  - `networkAcls.defaultAction: Allow` contradicts the NSP `Enforced` mode, so pick one network model
  - `Standard_LRS` is enough for a playground (currently `Standard_RAGRS`)
- [ ] **Least privilege:**
  - Narrow the deployment service principal from subscription **Owner** to Contributor + User Access Administrator (with conditions), scoped to what's needed.
  - The API only publishes, so it could get *Azure Service Bus Data Sender* if you pre-create the topology. MassTransit's `ConfigureEndpoints` needs Manage rights, so *Data Owner* is a known trade-off.
- [ ] **Key Vault is deployed but nothing uses it.** Either use it (e.g. for the App Insights connection string) or remove it.
- [ ] **Scale the consumer to zero** with a KEDA `azure-servicebus` scale rule using managed identity.
- [ ] **Right-size the containers.** 1 vCPU / 2 GiB each is a lot for this workload.
- [ ] **Declare the container-app → ACR pull dependency in Bicep** so the first deploy pulls the image without the CLI workaround in the README.
- [ ] **Clean up the README:**
  - It contains subscription, tenant and object IDs, plus a stale resource group name (`MassTransit-Playground`). These aren't secrets, but they don't belong in the README.
  - Rewrite it to cover architecture, local setup and deployment, not one-off commands.
