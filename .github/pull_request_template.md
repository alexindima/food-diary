## Summary

- 

## Validation

- [ ] `dotnet` / `npm` checks relevant to the change were run
- [ ] deploy or runtime-impacting changes were verified locally or on staging where appropriate

## API Contract Review

- [ ] no backend HTTP contract change
- [ ] backend HTTP contract changed intentionally and relevant snapshots were reviewed/updated
- [ ] OpenAPI / error / payload snapshot impact was checked
- [ ] status-code, auth, and error-shape changes are called out below if applicable

Reference:

- `BACKEND_API_CONTRACT_GOVERNANCE.md`

Notes:

- 

## Security Review

Mark each touched area as `ok`, `risk accepted`, `follow-up`, or `n/a`.

- Authentication and session flows:
- Admin surface:
- Upload and asset flows:
- Telegram and external adapters:
- Proxy, network, and request trust:
- Data mutation safety:
- Secrets and deployment:
- Dependency and package posture:

Reference:

- `BACKEND_SECURITY_HARDENING.md`

## Deploy / Operations Notes

- [ ] no deploy/runtime impact
- [ ] requires staging verification
- [ ] requires release/staging promotion security checklist
- [ ] requires secret/config change
- [ ] requires migration or data-shape review

Notes:

- 

## Risk Acceptance / Follow-Up

- 
