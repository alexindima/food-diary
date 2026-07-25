#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -eq 0 ]; then
  echo "At least one image name=digest pair is required." >&2
  exit 64
fi

: "${IMAGE_PREFIX:?IMAGE_PREFIX must be set}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY must be set}"

certificate_identity="https://github.com/${GITHUB_REPOSITORY}/.github/workflows/deploy.yml@refs/heads/master"
certificate_issuer="https://token.actions.githubusercontent.com"

for image in "$@"; do
  name="${image%%=*}"
  digest="${image#*=}"

  if [ -z "$name" ] || [ "$digest" = "$image" ] || [[ ! "$digest" =~ ^sha256:[0-9a-f]{64}$ ]]; then
    echo "Invalid image name=digest pair: $image" >&2
    exit 65
  fi

  reference="${IMAGE_PREFIX}/${name}@${digest}"
  media_type=$(docker buildx imagetools inspect "$reference" --raw | jq -r '.mediaType // empty')

  case "$media_type" in
    application/vnd.oci.image.index.v1+json|application/vnd.docker.distribution.manifest.list.v2+json)
      ;;
    *)
      echo "$reference must resolve to an image index, got: ${media_type:-missing media type}" >&2
      exit 66
      ;;
  esac

  cosign sign --yes "$reference"
  cosign verify \
    --certificate-identity "$certificate_identity" \
    --certificate-oidc-issuer "$certificate_issuer" \
    "$reference" >/dev/null

  echo "Verified signed image index: $reference"
done
