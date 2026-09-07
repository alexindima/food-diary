# Independent services

- Keep BugTriage, MailInbox and MailRelay in their own physical directory under `Services/`, matching their solution folders.
- Each service owns its host, persistence and runtime configuration. Directory grouping does not introduce shared business ownership or permission to reference another service's internals.
- Cross-service calls use approved client packages. Preserve the existing dependency matrix and layer-specific guides.
- Keep Docker build contexts rooted at the repository; service-specific Dockerfiles and deployment scripts use their full `Services/<name>/...` source paths.
