---
id: system.runtime-topology
kind: system
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiRuntimeTopology.ps1
sources:
  - .llm-wiki/generated/runtime-topology.json
  - docker-compose.yml
---

# Runtime Topology

The generated topology inventories Docker Compose services, hosted workers,
typed or direct `HttpClient` consumers, webhook-related types, and recurring job
registrations. Compose records include declared ports, profiles, networks,
environment-key names, mounts, dependencies, and selected container-hardening
flags. Webhook and outbound-network records include inferred security signals.

Every record distinguishes repository declarations or code inference from
runtime proof. The topology cannot establish effective production exposure,
cloud IAM or database grants, DNS answers at connect time, proxy/redirect
behavior, certificate validation, or webhook replay/idempotency. Deployed
topology, provider dashboards, runtime probes, and environment-specific
infrastructure remain authoritative.
