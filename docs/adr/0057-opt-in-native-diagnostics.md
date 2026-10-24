# 0057: Native incident diagnostics remain opt-in and bounded

Status: Accepted

Date: 2026-10-24

## Context

Job Manager intermittently exits with a kernel-reported native fault and exit
139. Managed logs identify recovery but cannot identify the native caller.
An earlier default-on crash-capture change was reverted after a regression.
Normal operation must not depend on diagnostic initialization or inherit a
diagnostic file-size limit. Crash artifacts can contain private process state.

## Decision

Keep the base image, direct dotnet entrypoint and default Compose configuration
free of crash capture. Provide a separately selected incident overlay for the
exact deployed image. It enables Triage rather than Heap/Full dumps and adds
SYS_PTRACE only to this temporary Job Manager instance, without privileged mode
or an unconfined seccomp profile. Production activation is an explicit operator
action, not part of the deployment workflow.

Use a root-only, 256 MiB Docker local tmpfs volume. A network-isolated retention
helper keeps that mount alive across application exits and removes old/excess
artifacts every minute: at most seven days and four matching files after cleanup.
The filesystem, rather than application RLIMIT_FSIZE, imposes the hard capacity.
Suppress a second uncontrolled OS core copy. Do not export artifacts to logs,
CI, source control, or audit journals. Authorized operators retrieve only the
incident pair into restricted storage and report sanitized stack conclusions.

## Consequences

The actual Compose overlay must pass a synthetic native-crash test, including
artifact survival across application restart, private modes, storage exhaustion
and retention. Default-off capture remains an architecture guardrail.
Volatile artifacts are lost when the last mount closes or the daemon/host
restarts. Full storage or faults bypassing the runtime handler can prevent
capture; no successful configuration check guarantees a future backtrace.
The overlay is diagnostic preparation, not a fix for an unknown native cause.

See [the operational runbook](../backend/PRODUCTION_RUNTIME_DIAGNOSTICS.md).
