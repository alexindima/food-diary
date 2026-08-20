#!/bin/sh
set -eu

server_directory=${MAIL_INBOX_POSTGRES_TLS_SERVER_DIRECTORY:-/server}
trust_directory=${MAIL_INBOX_POSTGRES_TLS_TRUST_DIRECTORY:-/trust}
server_name=${MAIL_INBOX_POSTGRES_TLS_SERVER_NAME:-mailinbox-postgres}
postgres_uid=${MAIL_INBOX_POSTGRES_UID:-70}
renew_before_seconds=${MAIL_INBOX_POSTGRES_TLS_RENEW_BEFORE_SECONDS:-2592000}

mkdir -p "$server_directory" "$trust_directory"

if [ -s "$server_directory/server.crt" ] &&
   [ -s "$server_directory/server.key" ] &&
   [ -s "$server_directory/ca.crt" ] &&
   [ -s "$trust_directory/ca.crt" ] &&
   openssl x509 -checkend "$renew_before_seconds" -noout -in "$server_directory/server.crt" >/dev/null 2>&1; then
    exit 0
fi

temporary_directory=$(mktemp -d)
trap 'rm -rf "$temporary_directory"' EXIT HUP INT TERM
umask 077

openssl req \
    -quiet \
    -x509 \
    -newkey rsa:3072 \
    -nodes \
    -sha256 \
    -days 3650 \
    -subj "/CN=FoodDiary MailInbox PostgreSQL CA" \
    -keyout "$temporary_directory/ca.key" \
    -out "$temporary_directory/ca.crt"

openssl req \
    -quiet \
    -newkey rsa:3072 \
    -nodes \
    -sha256 \
    -subj "/CN=$server_name" \
    -keyout "$temporary_directory/server.key" \
    -out "$temporary_directory/server.csr"

cat >"$temporary_directory/server.ext" <<EOF
basicConstraints=critical,CA:FALSE
keyUsage=critical,digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=DNS:$server_name
EOF

openssl x509 \
    -req \
    -sha256 \
    -days 825 \
    -in "$temporary_directory/server.csr" \
    -CA "$temporary_directory/ca.crt" \
    -CAkey "$temporary_directory/ca.key" \
    -CAcreateserial \
    -extfile "$temporary_directory/server.ext" \
    -out "$temporary_directory/server.crt"

cp "$temporary_directory/server.crt" "$server_directory/server.crt"
cp "$temporary_directory/server.key" "$server_directory/server.key"
cp "$temporary_directory/ca.crt" "$server_directory/ca.crt"
cp "$temporary_directory/ca.crt" "$trust_directory/ca.crt"

chmod 0444 "$server_directory/server.crt" "$server_directory/ca.crt" "$trust_directory/ca.crt"
chmod 0400 "$server_directory/server.key"
chown "$postgres_uid:$postgres_uid" \
    "$server_directory/server.crt" \
    "$server_directory/server.key" \
    "$server_directory/ca.crt"
