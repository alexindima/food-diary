# Temporary dependency constraints

## NuGet cryptography packages

Reviewed on 2026-10-15. `Directory.Packages.props` sets
`System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml` to
`10.0.13`. Explicit security references constrain their versions at the existing
parent consumers:

- `Pkcs`: Admin infrastructure and the BugTriage, MailInbox, and MailRelay
  infrastructure projects that reference MailKit.
- `Xml`: full persistence composition, Identity infrastructure, and Wearables
  infrastructure that reference ASP.NET Core DataProtection.

Dependent hosts and tests inherit these constraints. Global central transitive
pinning remains disabled so unrelated central package versions do not change
transitive resolutions.

The architecture guards retain their exact package sets and explicitly permit
these security references in full persistence composition and mail service
infrastructure. Remove those exact guard entries with the corresponding
references when the parent packages supply patched versions themselves.

The current dependency graph brings in `Pkcs` through `MimeKit 4.17.0` and
`Xml`, and brings in `Xml` through `Microsoft.AspNetCore.DataProtection
10.0.11`. The packages reach mail services, Admin infrastructure, application
hosts, and their tests. NuGet reports denial-of-service vulnerabilities in
versions through `10.0.12`:

- `Pkcs`: [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)
  and [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx).
- `Xml`: [GHSA-7hqp-7c5m-4vq6](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6),
  [GHSA-c399-9853-6jmj](https://github.com/advisories/GHSA-c399-9853-6jmj), and
  [GHSA-r955-8q5j-3h83](https://github.com/advisories/GHSA-r955-8q5j-3h83).

Both packages have fixes in `10.0.13`. Even `MimeKit 4.18.1`, the latest stable
release at this review, still declares `Pkcs 10.0.0`. The two transitive pins
keep the remediation bounded while preserving current parent dependency
versions and vulnerability enforcement.

Remove each security reference and its central version after updating all of
its parent dependencies so a fresh
resolution without the pin selects a patched version on every target framework.
Verify the regenerated lockfiles, a transitive NuGet vulnerability audit, and
the existing consumer tests before removal. A package audit alone does not establish
whether an application exposes a vulnerable operation.
