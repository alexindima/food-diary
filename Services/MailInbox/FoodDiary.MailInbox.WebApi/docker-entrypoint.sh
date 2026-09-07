#!/bin/sh
set -eu

if [ "$(id -u)" -ne 0 ]; then
    exec "$@"
fi

: "${APP_UID:?APP_UID must be configured}"

certificate_source=${MAIL_INBOX_TLS_CERTIFICATE_SOURCE:-/run/letsencrypt/live/mail.fooddiary.club/fullchain.pem}
private_key_source=${MAIL_INBOX_TLS_PRIVATE_KEY_SOURCE:-/run/letsencrypt/live/mail.fooddiary.club/privkey.pem}

tls_directory=/run/mail-inbox-tls
umask 077
mkdir -p "$tls_directory"
chmod 0700 "$tls_directory"
cp "$certificate_source" "$tls_directory/fullchain.pem"
chmod 0400 "$tls_directory/fullchain.pem"
chown "$APP_UID:$APP_UID" "$tls_directory/fullchain.pem"
cp "$private_key_source" "$tls_directory/privkey.pem"
chmod 0400 "$tls_directory/privkey.pem"
chown "$APP_UID:$APP_UID" "$tls_directory/privkey.pem"
chown "$APP_UID:$APP_UID" "$tls_directory"

exec su-exec "$APP_UID:$APP_UID" "$@"
