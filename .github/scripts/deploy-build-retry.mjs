import { spawnSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

export function isMissingGitHubCacheBlob(record) {
    if (record?.Status !== "failed" || typeof record.Error?.Message !== "string") return false;
    const message = record.Error.Message;
    const match = message.match(/^(?:failed to solve:\s*)?failed to copy:\s*GET\s+(https:\/\/[^\s]+)/u);
    if (!match || !/\bBlobNotFound\b/u.test(message)) return false;
    try {
        const url = new URL(match[1]);
        return /^[a-z0-9-]+\.blob\.core\.windows\.net$/u.test(url.hostname) &&
            url.pathname.startsWith("/actions-cache/") && !url.username && !url.password;
    } catch {
        return false;
    }
}

export function inspectFailedBuild(builder, run = spawnSync) {
    if (!/^[a-z0-9][a-z0-9_.-]{0,127}$/u.test(builder ?? "")) throw new Error("A valid isolated build builder is required.");
    // This builder belongs to this matrix job and has run exactly one build.
    // Do not print raw history: cache URLs may contain signed query parameters.
    const result = run("docker", ["buildx", "history", "inspect", "--builder", builder, "--format", "json"], {
        encoding: "utf8", timeout: 30_000, maxBuffer: 4 * 1024 * 1024,
        stdio: ["ignore", "pipe", "pipe"],
    });
    if (result.error || result.status !== 0) throw new Error("Cannot confirm the cache failure from Buildx history; automatic retry refused.");
    try {
        return JSON.parse(result.stdout);
    } catch {
        throw new Error("Invalid Buildx history; automatic retry refused.");
    }
}

export function selectBuildDigest(primaryOutcome, primaryDigest, retryOutcome, retryDigest) {
    let digest;
    if (primaryOutcome === "success" && retryOutcome === "skipped") digest = primaryDigest;
    else if (primaryOutcome === "failure" && retryOutcome === "success") digest = retryDigest;
    else throw new Error("No single successful production build; signing and deployment refused.");
    if (!/^sha256:[0-9a-f]{64}$/u.test(digest ?? "")) throw new Error("Successful build did not produce a valid immutable image digest.");
    return digest;
}

export function shouldExportCache(startedAt, nowSeconds = Math.floor(Date.now() / 1000)) {
    if (typeof startedAt !== "string" || !/^\d+$/u.test(startedAt)) return false;
    const start = Number(startedAt);
    // Reserve five minutes for the optional two-minute step and job cleanup.
    return Number.isSafeInteger(start) && Number.isSafeInteger(nowSeconds) &&
        nowSeconds >= start && nowSeconds - start < 25 * 60;
}

function output(name, value) {
    if (!process.env.GITHUB_OUTPUT) throw new Error("GITHUB_OUTPUT is required.");
    appendFileSync(process.env.GITHUB_OUTPUT, `${name}=${value}\n`);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    try {
        switch (process.argv[2]) {
            case "classify": {
                const record = inspectFailedBuild(process.env.BUILDER_NAME);
                if (!isMissingGitHubCacheBlob(record)) throw new Error("Build failure is not a confirmed missing GitHub Actions cache blob; automatic retry refused.");
                output("retry", "true");
                console.log("::warning::GitHub Actions cache blob is missing; retrying once in a fresh builder without cache.");
                break;
            }
            case "select": {
                const digest = selectBuildDigest(process.env.PRIMARY_OUTCOME, process.env.PRIMARY_DIGEST,
                    process.env.RETRY_OUTCOME, process.env.RETRY_DIGEST);
                output("digest", digest);
                break;
            }
            case "cache-budget": {
                output("allowed", shouldExportCache(process.env.BUILD_STARTED_AT) ? "true" : "false");
                break;
            }
            default:
                throw new Error("Expected classify, select or cache-budget command.");
        }
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
