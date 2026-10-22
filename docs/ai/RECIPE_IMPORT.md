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
readable. Public YouTube watch, Shorts and youtu.be video links automatically
select **Recognize speech from video** when no recipe text is pasted. This selection
is visible before recognition and makes no provider request until the user chooses
**Recognize recipe**. Users can turn video speech off for the current URL and paste
recipe text instead; a new source URL resets that opt-out. Explicitly enabling video
speech keeps it enabled when captions or the URL change. For other public video
URLs or a saved MP4/WebM file, enable it manually. Text displayed in video frames
(OCR) is not read.

## Video speech

`POST /api/v{version}/ai/food/recipe-import/video` accepts multipart form data:
optional `video`, `sourceUrl`, and `text`. A video file or URL is required; the
additional caption is limited to 8,000 characters. A supplied file takes precedence
over fetching the URL. The URL is retained as provenance.

Video input is bounded at 50 MiB. The loader reads direct MP4/WebM URLs or video
metadata (`og:video`, video/source tags) from public HTML. It does not execute page
scripts or use Instagram credentials. Instagram can refuse this access; uploading
the saved video or pasting its caption remains the fallback.

Public YouTube watch, Shorts and youtu.be links resolve an MP4/WebM audio-only
stream through pinned YoutubeExplode 6.6.2. IDs retain their case. Metadata must
state a positive duration up to five minutes before downloading; the decoded WAV
duration is checked again. The stream retains the 50 MiB input cap. Unknown/live,
unavailable or restricted videos are rejected, without account cookies. Site or
network restrictions can still prevent access. Source failures have localized
guidance distinct from invalid media, size or duration errors.

The dedicated YouTube HTTP client permits HTTPS/443 for YouTube API/page hosts
and googlevideo.com subdomains only, including library redirects. It retains
public-DNS connection checks and bounds metadata at four MiB and ten seconds.
Media passes through the shared byte limit. FFmpeg still reads a generated local
file; no downloader executable or JavaScript runtime is added. Tests mock YouTube
transport and all AI provider calls.

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
reserves a conservative estimate of 64 quota units per second in each direction:
input audio and output transcript. Both budgets must be positive, and both user
limits apply before transcription. The text token counter cannot count audio.
Reported token usage replaces the estimate; absent or
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
qualifiers, into text ingredients. Ingredient `name` contains the food name and
preparation notes; `amount` contains the quantity and unit, without repeating the
name. For example, `2 tsp cornflour / cornstarch` becomes `cornflour / cornstarch`
and `2 tsp`. The provider adapter repairs clear duplicated quantity/name text
using recognized units without converting measurements. Correctly separated
fields are preserved. An uncertain duplicated source line is retained in the name
with an empty amount rather than guessing a quantity or losing source text.
Each source ingredient row is attached once
to its first evidenced cooking step using a zero-based `stepIndex`. Separate
rows for the same ingredient in different mixtures retain their quantities and
step assignments. An unknown assignment (or a legacy response without it) falls
back to the first step. It does not estimate weights or select catalogue products. Missing
times remain empty; missing servings use the editor's default of one with an
explicit notice. Missing instructions require the user to fill the empty step.
Nutrition quoted by the author is kept as an unverified comment with its stated
basis. The optional structured `nutrition` also copies the stated kcal and grams,
without estimating or recalculating nutrients. Complete calories, proteins, fats
and carbs with an explicit `recipe` or `serving` basis populate manual nutrition.
Per-serving values require an explicit serving count and are multiplied once to
whole-recipe totals within the existing manual limits. Import resets the editor's
display basis to the whole recipe. Explicit zeroes are retained; absent optional
fiber/alcohol stay empty. Unknown, incomplete and `per100g` nutrition remains a
note: no recipe mass is inferred. Users can link ingredients to products or enter
nutrition manually before saving. Recognition does not
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
