# Production log rotation and temporary native crash diagnostics

## Nginx rotation

The hardened Nginx master runs as root without `DAC_OVERRIDE`. The host's former
`create 0640 www-data adm` policy produced files that master could not reopen.
The managed policy creates `0640 root adm` files and sends `USR1` to the
`fooddiary-nginx-1` container after rotation. Rotation retains fourteen generations,
compression, delayed compression and the existing prerotate hook.

Deploy copies `scripts/deploy/install_nginx_logrotate.py` and
`nginx-logrotate.conf`, installs the policy and checks logrotate syntax **before
stopping writers**. The installer accepts the observed Debian policy or the
already-installed managed policy. An unknown configuration fails without being
overwritten. The original is retained at
`/var/lib/fooddiary/nginx-logrotate.original` with mode `0600`. No second wildcard
policy is installed, so logrotate does not acquire overlapping ownership.

After an authorized rollout, verify a scheduled rotation: the active access log
must receive fresh requests and master must stop holding its previous `.1` file.
Do not force all production rotations merely to check installation. The Linux
boundary test performs two forced rotations in isolated fixtures and reproduces
the original permission failure separately.

To roll back, first review the saved original and the current managed policy,
then restore only `/etc/logrotate.d/nginx`. Restoring the old owner also restores
its known incompatibility; avoid treating rollback as a repair.

## Job Manager: explicit incident overlay

Default Compose and the application image still disable crash capture. The
September automatic-capture implementation was reverted after a regression.
The new overlay is **not copied or activated by normal deploys**. It changes no
job schedules, business handlers, application file-size limits or release policy.

Before enabling it, obtain the exact deployed Job Manager image digest from
trusted deployment metadata, make the reviewed script and overlay available
under `/opt/fooddiary`, and schedule a brief Job Manager recreation. Never infer
this image digest from the API's commit SHA alone. This is a production change;
preparing this repository change does not activate it.

The following commands illustrate an authorized incident session. Use the actual
reviewed digest in the existing environment; do not copy credentials or render
the full merged Compose configuration into reports.

```sh
cd /opt/fooddiary
test -n "$JOB_MANAGER_IMAGE_REF"
docker compose -f docker-compose.yml -f docker-compose.job-manager-diagnostics.yml \
  --profile full up -d --wait --no-deps job-manager-diagnostics-retention
docker compose -f docker-compose.yml -f docker-compose.job-manager-diagnostics.yml \
  --profile full up -d --no-deps --force-recreate job-manager
```

Only the diagnostic Job Manager gains `SYS_PTRACE`; it remains read-only and
unprivileged, with the normal seccomp policy. OS core dumps are disabled for this
session to avoid a second, uncontrolled copy through the host core handler. The
runtime produces a Triage dump (`DOTNET_DbgMiniDumpType=3`) and a JSON crash report.
The entrypoint sets `umask 077`; directory mode is `0700`, files are `0600`.
No file-size resource limit is imposed on normal application writes.

Artifacts can still contain sensitive stack values and native state. They are
for authorized incident responders only. Never print or attach their contents to
logs, Grafana, CI, GitHub, BugTriage or the repository. Only sanitized conclusions
belong in an audit report. Triage is data minimization, not guaranteed redaction.

## Storage, retention and failure behavior

- A dedicated Docker local tmpfs volume has a hard **256 MiB** capacity, separate
  from the application's `/tmp`, root filesystem and database volumes.
- The network-isolated retention helper keeps the mount alive across Job Manager
  exits and recreations. Stopping the last user of this volume or restarting the
  Docker daemon/host loses the volatile artifacts. Retrieve needed evidence
  promptly to restricted operator storage; this is not durable backup storage.
- At startup and every minute, the helper removes matching regular files older
  than seven days and retains the newest four matching files. A dump/report pair
  normally uses two files. Active captures can temporarily exceed the count,
  while the filesystem capacity remains the hard bound. Age cleanup can lag by
  one minute. The helper never follows symlinks or cleans other volumes.
- Full storage may prevent a useful dump; it does not limit application file
  writes. Validate the diagnostic session with the fixture before activation.
  A successfully configured session cannot guarantee capture of every native
  fault, including faults that bypass the runtime signal handler.

After a real recurrence, correlate UTC timestamp, exact image digest, exit code
and OOM flag. List only artifact names, sizes and permissions. Retrieve only the
matching pair into restricted storage outside the repository before stopping
the helper. Analyze native frames with compatible LLDB/SOS and symbols. Do not
change the runtime or base image solely because a fault mentions `ld-musl`.

To disable capture, recreate only Job Manager with the **base** Compose file and
the same deployed image, then stop/remove only the owned retention helper. Delete
the exact diagnostic volume only after verifying no investigation still needs
it. Delete retrieved copies when the investigation ends, within seven days.
No production crash should be triggered for testing.

A normal deploy also recreates Job Manager from base Compose and ends capture.
The retention helper may remain as an orphan until explicitly removed. Treat
capture as a temporary incident session and check its state after each release.

## Verification

```powershell
python -B -m unittest discover -s scripts/deploy -p test_install_nginx_logrotate.py
$env:FOODDIARY_RUNTIME_DOCKER_TESTS='1'
python -B -m unittest discover -s scripts/deploy -p test_runtime_diagnostics_docker.py
```

The Docker suite verifies baseline failure, repeated managed rotation, a real
synthetic native crash, actual artifacts surviving application restart, private
file modes, full diagnostic storage with independent application writes, and
age/count cleanup. Its processes contain synthetic fixture data only. CI never
uploads the diagnostic volume.

References: [Nginx signals and rotation](https://nginx.org/en/docs/control.html),
[.NET crash dump configuration](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash),
[CoreCLR native dump implementation](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md).
