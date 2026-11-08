// MinIO's public images were withdrawn. Silo preserves the S3/MINIO_* protocol
// and the MC command used by initialization and cleanup evidence.
// Provenance: https://github.com/pgsty/silo/releases/tag/RELEASE.2026-10-26T00-00-00Z
// Anonymous pulls and S3 semantics are checked by verify-storage-images.mjs.
export const storageImages = Object.freeze({
    server: 'docker.io/pgsty/silo@sha256:7bb2aff6b89efe1cb1303c10fc21d579447434ed06649ac9ec05a5e2dbf30ad7',
    client: 'docker.io/pgsty/mc@sha256:ce62c9053c69ce12a527a322345b5ad9f44b5f8a7b8189f82e516f2e3f30f1d8',
});
