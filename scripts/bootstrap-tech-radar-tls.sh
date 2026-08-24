#!/bin/sh
set -eu

domain=techradar.fooddiary.club
webroot=/var/www/fooddiary.club
certificate_name=fooddiary.club

if ! command -v certbot >/dev/null 2>&1; then
    echo "certbot is not installed."
    exit 1
fi

resolved_ipv4=$(getent ahostsv4 "$domain" | awk 'NR == 1 { print $1 }')
if [ -z "$resolved_ipv4" ]; then
    echo "$domain does not resolve to an IPv4 address yet."
    exit 1
fi

mkdir -p "$webroot/.well-known/acme-challenge"

certbot certonly \
    --webroot \
    --cert-name "$certificate_name" \
    --expand \
    --non-interactive \
    --webroot-path /var/www/fooddiary.club \
    -d fooddiary.club \
    -d www.fooddiary.club \
    --webroot-path /var/www/admin.fooddiary.club \
    -d admin.fooddiary.club \
    --webroot-path "$webroot" \
    -d "$domain"

certbot certificates --cert-name "$certificate_name"
docker exec fooddiary-nginx-1 nginx -t
docker exec fooddiary-nginx-1 nginx -s reload
