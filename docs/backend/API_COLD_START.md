# API cold-start publishing and measurements

The API production Dockerfile publishes framework-dependent ReadyToRun assemblies
for the container's architecture. Its pinned SDK and runtime base images remain
unchanged. `PUBLISH_READY_TO_RUN=true` is the default; building with
`--build-arg PUBLISH_READY_TO_RUN=false` produces an IL control with the same
runtime identifier, package graph and runtime image. JobManager publishing and
application startup behavior are unchanged. No application warmup is added.

ReadyToRun reduces first-use JIT compilation; it does not precompile every dynamic
code path or initialize EF models and application caches. Compare cold and warm
requests before attributing an observed delay to one of these costs.
See [Microsoft's ReadyToRun guide](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run).

## Dependency locking

Ordinary builds retain `.nuget/lockfiles/<project>.packages.lock.json`.
The API container sets `FoodDiaryRuntimeLockIdentifier` to the same RID as
`--runtime`. The property selects `.nuget/lockfiles/<RID>/<project>.packages.lock.json`
through the root `Directory.Build.props` before NuGet evaluates the restore graph.
The committed platform graph currently covers
`linux-musl-x64`, matching the production Linux x64 container.

Restore remains in `--locked-mode`. Both control and ReadyToRun builds restore
the ReadyToRun toolchain before publishing with `--no-restore`; changing only the
publish flag preserves the restored inputs for a controlled comparison.

When updating API dependencies, regenerate the platform graph explicitly:

```powershell
dotnet restore FoodDiary.Web.Api/FoodDiary.Web.Api.csproj --runtime linux-musl-x64 --force-evaluate -p:PublishReadyToRun=true -p:FoodDiaryRuntimeLockIdentifier=linux-musl-x64
```

Review these files with the generic lockfiles. The architecture guard rejects
platform lockfiles that change any generic target-framework dependency graph.
Preparing another container architecture requires its own platform lockfiles.

## Reproduce the comparison

Run from the repository root with Python 3.12+, .NET 10 and Linux Docker ready.
The benchmark creates its own labelled PostgreSQL, Redis, MinIO and OpenTelemetry
containers. It uses the real production API, the normal initializer and a synthetic
account with health-tracking consent. All credentials and cookies remain in the
ignored workspace. The existing local application and production are not used.

```powershell
pwsh -NoProfile -File scripts/Ensure-Docker.ps1
$r2rArtifacts = Join-Path (Get-Location) '.artifacts/readytorun'
$r2rInitializer = Join-Path $r2rArtifacts 'initializer-build'
$r2rWorkspace = Join-Path $r2rArtifacts 'benchmark'
dotnet build FoodDiary.Initializer/FoodDiary.Initializer.csproj --artifacts-path $r2rInitializer -m:2

docker build --platform linux/amd64 -f FoodDiary.Web.Api/Dockerfile --build-arg PUBLISH_READY_TO_RUN=false -t fooddiary-api:r2r-off .
docker build --platform linux/amd64 -f FoodDiary.Web.Api/Dockerfile --build-arg PUBLISH_READY_TO_RUN=true -t fooddiary-api:r2r-on .

python scripts/performance/benchmark_api_readytorun.py prepare --workspace $r2rWorkspace --baseline-image fooddiary-api:r2r-off --initializer "$r2rInitializer/bin/FoodDiary.Initializer/debug/FoodDiary.Initializer.dll"
python scripts/performance/benchmark_api_readytorun.py measure --workspace $r2rWorkspace --baseline-image fooddiary-api:r2r-off --candidate-image fooddiary-api:r2r-on --repetitions 5
python scripts/performance/benchmark_api_readytorun.py cleanup --workspace $r2rWorkspace
```

The runner checks the API assembly's actual ReadyToRun native header before
measuring. Each trial starts a fresh API process limited to two CPUs and 768 MiB.
It clones the same prepared database and clears only its owned Redis. Variants
alternate order between repetitions. An owned volume preserves Data Protection
keys between API processes, matching production session-cookie key persistence.
A local OTLP collector retains the normal
host instrumentation, with a one-second metric export interval for both variants.

After the existing readiness check succeeds, the measured first-open sequence is:

1. Restore the authenticated browser session.
2. Read notifications, unread count, push configuration, privacy and dashboard in parallel.
3. Measure the first diary and product overview reads separately.
4. Repeat the read endpoints three times to observe warm behavior.

Every measured request must return HTTP 200. Failed requests abort the comparison;
missing runtime metrics are errors, not zero JIT cost. The dashboard uses one
fixed calendar date and UTC offset for the run. Setup, fixture consent and login
occur outside the timed trials.

`results.json` records image identities, build-input hashes, request timings,
runtime metrics and RSS/high-water memory. Logs contain only the synthetic run's
data. If a run fails, inspect its local logs and execute the same `cleanup` command.
Cleanup verifies ownership labels before removing containers, the network and
the key volume, and deletes the
generated credential files; measurement evidence remains available.

## Measured baseline, 2026-11-03

Five cold-process trials per variant were executed from revision `041468621`
with the ReadyToRun build changes. The complete generic dependency graphs in all
278 platform lockfiles matched their existing counterparts.

| Median measurement | IL control | ReadyToRun |
| --- | ---: | ---: |
| Readiness, including container launch | 2.76 s | 2.50 s |
| First-open API sequence after readiness | 3.61 s | 3.28 s |
| First dashboard request | 2.07 s | 2.11 s |
| JIT time through cold section reads | 4.19 s | 2.77 s |
| Process RSS after cold section reads | 282.6 MiB | 319.1 MiB |
| Runtime image size | 144.9 MiB | 175.0 MiB |

The measured median first-open API sequence improved by approximately 9%, with
36.5 MiB additional RSS. First-open samples were 3.58, 4.29, 3.65, 3.61 and 3.47 s
for the control, and 11.37, 3.81, 2.89, 2.78 and 3.28 s for ReadyToRun. The slow
ReadyToRun sample is retained; its cause was not established. These small samples
do not establish a tail-latency improvement or a guaranteed production benefit.
No repository builds or verification suites ran concurrently with this final batch.
The original portable production image was not used as
the timing control: both measured variants use identical platform publishing,
so the comparison isolates ReadyToRun rather than runtime asset selection.

These are local backend measurements with a synthetic account and no food history.
They exclude browser downloads, rendering and Internet latency. Operating-system,
storage and PostgreSQL caches are not cleared, and the production host's paging
pressure is not reproduced. The result supports a partial cold-start improvement;
production first-opening latency still requires a separate observation after an
authorized deployment. Cold dashboard work remains a useful profiling target.
