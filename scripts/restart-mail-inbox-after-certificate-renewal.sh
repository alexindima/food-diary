#!/bin/sh
set -eu

if [ "${RENEWED_LINEAGE:-}" != "/etc/letsencrypt/live/mail.fooddiary.club" ]; then
    exit 0
fi

cd /opt/fooddiary
/usr/bin/docker compose restart mail-inbox
