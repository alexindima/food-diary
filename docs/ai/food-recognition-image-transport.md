# Food recognition image transport

Image uploads still use S3-compatible storage: MinIO for local development and
AWS S3 in production. Upload confirmation validates and publishes the object.

Recognition resolves the asset through `IImageAssetContentService`, an Images
owner capability. It checks ownership and confirmation, reads the published
object by its stored key, and returns a transient base64 data URL. OpenAI receives
the same content for token counting and recognition, including fallback model
token counting. The provider no longer needs to reach the stored public URL;
local loopback URLs cannot be fetched from OpenAI's servers.

Reads have a 30-second deadline, propagate caller cancellation, enforce the
configured upload limit (at most 50 MiB), and reject unsupported MIME types or
incomplete content. No arbitrary HTTP image fetch is introduced. Image content
must not be logged, persisted in jobs, or included in errors. Jobs continue to
store asset IDs. Browser image URLs and upload contracts are unchanged.

This adds a backend object read and base64 request overhead. Both API and
JobManager need the existing S3 configuration and read access to the published
bucket. Restart both hosts after rebuilding. Verification uses offline storage
and HTTP fakes; a local end-to-end retry is still needed to confirm the configured
provider and storage environment.

The OpenAI [token counting guide](https://developers.openai.com/api/docs/guides/token-counting)
documents base64 data URLs as supported image input.
