# Production deployment

`techradar.fooddiary.club` runs as the read-only `tech-radar` Compose service on `127.0.0.1:4210`. The public Nginx container terminates TLS and proxies to that loopback port.

## One-time bootstrap

1. Create an `A` record for `techradar.fooddiary.club` pointing to the same public IPv4 address as `fooddiary.club`. Do not enable an HTTP proxy at the DNS provider during ACME bootstrap.
2. Wait until `getent ahostsv4 techradar.fooddiary.club` on `fooddiary-prod` resolves to the production server.
3. Deploy the HTTP virtual host. Its ACME challenge reuses the existing `/var/www/fooddiary.club` webroot.
4. On `fooddiary-prod`, run `/opt/fooddiary/scripts/bootstrap-tech-radar-tls.sh` as root. The script expands the existing `fooddiary.club` certificate while retaining `fooddiary.club`, `www.fooddiary.club`, and `admin.fooddiary.club`.
5. Confirm that `certbot certificates --cert-name fooddiary.club` lists `techradar.fooddiary.club` and that `https://techradar.fooddiary.club` serves a valid certificate.

Never run Certbot with only the new domain against the existing certificate name: omitting current SANs would remove their coverage.

## Regular deployment

The standard production workflow builds, signs, pushes, pulls, and starts the radar image together with the other production images. It then checks the container health endpoint before recreating Nginx.

## Verification

```sh
docker compose --profile full ps tech-radar
curl -fsS http://127.0.0.1:4210/ >/dev/null
curl -fsSI https://techradar.fooddiary.club/
openssl s_client -connect techradar.fooddiary.club:443 -servername techradar.fooddiary.club </dev/null 2>/dev/null \
  | openssl x509 -noout -subject -issuer -dates -ext subjectAltName
```

## Rollback

Set `TECH_RADAR_IMAGE_REF` in `/opt/fooddiary/.env` to the last known-good digest and recreate only the radar service:

```sh
docker compose --profile full up -d --force-recreate --no-deps tech-radar
```

The service has no database, migrations, volumes, or API dependency. Removing its Nginx virtual host and stopping the container fully removes the public surface.
