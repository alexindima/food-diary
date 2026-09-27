# Job Manager Crash Dumps

## Purpose

Job Manager has experienced intermittent native `SIGSEGV` exits before managed
logging can capture a stack. Production enables .NET Triage dumps only for this
service so an operator can identify the native and managed callers after a
recurrence. This is diagnostic evidence, not an automatic recovery mechanism.

## Storage and privacy boundary

Crash artifacts are stored in the dedicated `job-manager-crash-dumps` Docker
volume, mounted only at
`/var/lib/fooddiary/job-manager-crashdumps` in Job Manager. The application root
filesystem remains read-only. The directory is owned by `root:root`, uses mode
`0700`, and files inherit a restrictive `0077` umask before being normalized to
mode `0600`.

Triage dumps exclude heap memory, but stack memory, module paths, native state,
and crash-report metadata can still contain sensitive values. Never print file
contents, upload artifacts to CI, attach them to GitHub, send them to Grafana or
Loki, or copy them into the repository. Docker/root access is the authorization
boundary; do not mount this volume into another long-running service.

## Bounds and lifecycle

- `DOTNET_DbgMiniDumpType=3` requests the privacy-reduced Triage format.
- `RLIMIT_FSIZE` limits each diagnostic file to 64 MiB.
- The entrypoint removes artifacts older than seven days.
- The entrypoint keeps at most four files, normally two dump/report pairs.
- Cleanup runs before every application start and once per hour.
- Normal deploys and automatic container restarts preserve the named volume.

The four-file bound limits retained files to at most 256 MiB after cleanup. One
new crash can transiently add another bounded dump before the restart cleanup.
If `createdump` reports that the file-size limit prevented a useful Triage dump,
change the limit only through a reviewed deployment update; do not switch to
Heap or Full dumps in production by default.

## Production verification

Deployment fails when the directory is missing, is not writable, is not owned by
`root:root`, does not use mode `0700`, or the expected .NET dump settings are
absent. This verification reads configuration only and never opens artifact
contents.

After deployment, a production operator may verify the bounded inventory without
reading files:

```sh
cd /opt/fooddiary
docker compose exec -T job-manager sh -c \
  'find /var/lib/fooddiary/job-manager-crashdumps -maxdepth 1 -type f -name "job-manager.*" -exec ls -ln {} +'
```

An empty result is expected until a crash occurs. Do not generate a deliberate
crash in production.

## Incident retrieval and analysis

1. Record the crash UTC timestamp, deployed commit, image digest, container ID,
   exit code, and OOM status without collecting user payloads.
2. List artifact names and sizes. Select the dump whose epoch timestamp matches
   the incident. Do not display the JSON report in shared terminals or logs.
3. Create a restricted incident directory outside the repository on an encrypted
   operator-controlled system. Copy only the selected dump and its matching
   `.crashreport.json` with `docker cp` from the running Job Manager container.
4. Analyze the dump with LLDB/SOS on the same architecture and Alpine/runtime
   versions. Use trusted symbol sources and record only sanitized stack evidence.
5. Restrict access to the named incident responders. Do not forward the artifact
   to an upstream issue unless it has been separately reviewed and sanitized.
6. Delete the retrieved copy immediately after the investigation. Remove the
   source artifacts from the Docker volume when the evidence is no longer needed;
   the automatic seven-day/four-file bounds are a backstop, not a reason to retain
   resolved incident data.

## Rollback

Disabling capture requires a reviewed commit that removes the .NET dump settings
and volume mount. A normal rollback to an earlier Compose revision stops creating
new artifacts but does not delete the existing named volume. Inspect and delete
that volume separately only after confirming that no active investigation needs
its contents.
