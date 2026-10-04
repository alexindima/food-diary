# Recipe import

On the new recipe form, expand **Start from a recipe**, paste a public HTTPS URL
or a caption, and choose **Recognize recipe**. Recognition fills the existing
editor; the user reviews and saves it through the Recipes API. Pasted text takes
precedence over loading the URL, which provides a fallback for gated social posts.
The existing text import remains available without AI.

## Supported sources

The server reads bounded public HTML, JSON or plain text. It prefers schema.org
Recipe JSON-LD, then the current page's Open Graph description, description metadata,
an unambiguous embedded caption, and visible page text. Multiple distinct embedded
captions without page metadata are rejected so recommendations cannot select a
different post. Instagram tracking parameters and VK away wrappers are handled.
No additional scraping service, subscription, cookies or Instagram login is used.
Public Instagram pages can still reject server requests; in that case the user
is asked to paste the caption. There is no guarantee that every public Reel is
readable. Enable **Recognize speech from video** to use a public video URL or
upload a saved MP4/WebM file. Text displayed in video frames (OCR) is not read.

## Video speech

`POST /api/v{version}/ai/food/recipe-import/video` accepts multipart form data:
optional `video`, `sourceUrl`, and `text`. A video file or URL is required; the
additional caption is limited to 8,000 characters. A supplied file takes precedence
over fetching the URL. The URL is retained as provenance.

Video input is bounded at 50 MiB. The loader reads direct MP4/WebM URLs or video
metadata (`og:video`, video/source tags) from public HTML. It does not execute page
scripts or use Instagram credentials. Instagram can refuse this access; uploading
the saved video or pasting its caption remains the fallback.

After checking AI consent, the backend downloads/copies the bounded file and runs
local FFmpeg with a forced MP4/WebM demuxer and local file/pipe protocols only.
Two request slots are admitted before multipart binding, with a 429 busy response
when occupied. Two processing slots, a 75-second processing deadline and a 40-second FFmpeg
deadline bound resource use. FFmpeg receives generated local paths, never a URL
or original filename; no shell is used. Cancellation kills its process tree.
Temporary media is deleted in a finally block; a cleanup failure emits only a
generic warning. Linux temporary directories have owner-only permissions.

The result is mono 16 kHz PCM WAV with original metadata removed, up to five minutes
and below 10 MiB. Longer audio is rejected rather than silently importing a clipped
recipe. Only this WAV is sent to `/v1/audio/transcriptions` using
`OpenAi:TranscriptionModel` (default `gpt-transcribe`). Speech and the optional
caption then enter the existing recipe extraction prompt. No video frames are sent.
The full operation has a three-minute deadline. Transcription is not automatically
retried after a provider/transport failure.

Transcription and recipe parsing have separate idempotent quota reservations. Audio
uses a conservative estimate of 64 quota units per second because the text token
counter cannot count audio. Reported token usage replaces the estimate; absent or
duration-only usage retains the estimate and is marked estimated by existing quota
telemetry. This is an application quota estimate, not a provider billing amount.
A completed transcription is reconciled even if no speech/recipe is found or the
later parsing call cannot proceed. Uploaded content is included in the request hash.

The API runtime image installs pinned FFmpeg; its temporary filesystem is 256 MiB
to cover two bounded uploads, local copies and extracted audio. The Nginx video
route allows 52 MiB, disables request disk buffering and allows 190 seconds for
the response. Other API route limits remain unchanged. Deploy the updated API
image, compose temporary-space setting and Nginx site configuration together.
Local development needs FFmpeg on PATH or
`RecipeVideo__FfmpegPath` set to its executable. No additional extraction service or
subscription is required. Run real media tests with `FOODDIARY_TEST_FFMPEG` set to
a local full FFmpeg executable; provider verification remains mocked.

The native file picker is used because the UI kit does not publish a video file
selection control. It has an explicit label and is disabled during recognition.

## Draft semantics

AI extracts the stated quantities, including fractions, pinches and to-taste
qualifiers, into text ingredients. The ingredient list is attached to the first
step once. It does not estimate weights or select catalogue products. Missing
times remain empty; missing servings use the editor's default of one with an
explicit notice. Missing instructions require the user to fill the empty step.
Nutrition quoted by the author is kept as an unverified comment with its stated
basis. It never becomes calculated recipe nutrition. Users can link ingredients
to products or enter nutrition manually before saving. Recognition does not
publish or persist a recipe.

## Clipboard

Clipboard suggestions are opt-in on the new recipe form. The preference is local
to the browser. A read follows the opt-in click; on subsequent focus/visibility
returns, the clipboard is read only when `clipboard-read` is already granted.
Browsers without that permission API retain the manual paste path. Only exact
Instagram post/Reel/TV URLs are suggested; tracking parameters are removed.
Dismissed links are suppressed for the browser session. Clipboard text is not
sent to the API by reading it or accepting a suggestion. Recognition requires
the separate button click. No background system monitoring or polling is used.

## Ownership and boundaries

`POST /api/v{version}/ai/food/recipe-import` accepts optional `sourceUrl` and `text`
(at least one required). It uses existing Premium authorization, AI rate limits,
idempotency, consent and quota reservation/reconciliation. AI owns the draft
contract and extraction. Recipes continues to own recipe writes. No persistence
or production project dependency changes are required. Video adds local FFmpeg
to the runtime image and optional executable/model configuration.

The extraction prompt is version-controlled alongside its strict provider schema;
it does not reuse the food-text prompt that estimates portions. The untrusted
source is a separate user message beneath developer instructions. Provider
response storage is disabled. Verification uses mocked provider responses.

The source client accepts HTTPS/443 without credentials, disables cookies,
proxies and automatic redirects, and limits requests to twenty seconds, three
redirects and one MiB. Every connection resolves and checks all DNS addresses,
then connects directly to a validated address; private/special addresses and
IPv4-mapped equivalents are rejected. Redirect targets are checked again.
The loader does not log source URLs, caption bodies or clipboard contents.
