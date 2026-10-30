import { readdir, readFile } from "node:fs/promises";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";

// One catalog drives scheduling and the required complete release manifest.
export const images = [
    { name: "api", file: "FoodDiary.Web.Api/Dockerfile", context: "." },
    { name: "initializer", file: "FoodDiary.Initializer/Dockerfile", context: "." },
    { name: "job-manager", file: "FoodDiary.JobManager/Dockerfile", context: "." },
    { name: "client", file: "FoodDiary.Web.Client/Dockerfile", context: "FoodDiary.Web.Client" },
    { name: "telegram-bot", file: "FoodDiary.Telegram.Bot/Dockerfile", context: "." },
    { name: "mail-relay", file: "Services/MailRelay/FoodDiary.MailRelay.WebApi/Dockerfile", context: "." },
    { name: "mailrelay-initializer", file: "Services/MailRelay/FoodDiary.MailRelay.Initializer/Dockerfile", context: "." },
    { name: "mail-inbox", file: "Services/MailInbox/FoodDiary.MailInbox.WebApi/Dockerfile", context: "." },
    { name: "mailinbox-postgres-tls-init", file: "Services/MailInbox/FoodDiary.MailInbox.WebApi/Dockerfile.postgres-tls-init", context: "." },
    { name: "mailinbox-initializer", file: "Services/MailInbox/FoodDiary.MailInbox.Initializer/Dockerfile", context: "." },
    { name: "bugtriage", file: "Services/BugTriage/FoodDiary.BugTriage.WebApi/Dockerfile", context: "." },
    { name: "tech-radar", file: "FoodDiary.TechRadar/Dockerfile", context: "FoodDiary.TechRadar" },
];

export function createMatrix() {
    return { include: images.map(({ name, ...image }) => ({ image: name, ...image })) };
}

function validateIdentity({ sourceSha, runId, runAttempt }) {
    if (!/^[a-f0-9]{40}$/u.test(sourceSha ?? "")) throw new Error("Invalid source SHA.");
    if (!/^[1-9][0-9]*$/u.test(runId ?? "")) throw new Error("Invalid workflow run ID.");
    if (!/^[1-9][0-9]*$/u.test(runAttempt ?? "")) throw new Error("Invalid workflow run attempt.");
}

export function createReceipt({ name, digest, sourceSha, runId, runAttempt }) {
    validateIdentity({ sourceSha, runId, runAttempt });
    if (!images.some(image => image.name === name)) throw new Error("Unknown production image.");
    if (!/^sha256:[a-f0-9]{64}$/u.test(digest ?? "")) throw new Error("Invalid image-index digest.");
    return { schemaVersion: 1, name, digest, sourceSha, runId, runAttempt };
}

export function collectReceipts(receipts, identity) {
    validateIdentity(identity);
    if (receipts.length !== images.length) throw new Error("Incomplete production image receipt set.");
    const byName = new Map();
    for (const receipt of receipts) {
        if (receipt?.schemaVersion !== 1) throw new Error("Unsupported image receipt schema.");
        const validated = createReceipt(receipt);
        for (const key of ["sourceSha", "runId"]) {
            if (validated[key] !== identity[key]) throw new Error(`Foreign image receipt ${key}.`);
        }
        // Rerunning only failed jobs reuses successful workers from earlier attempts.
        if (BigInt(validated.runAttempt) > BigInt(identity.runAttempt)) throw new Error("Future image receipt attempt.");
        if (byName.has(validated.name)) throw new Error("Duplicate production image receipt.");
        byName.set(validated.name, validated.digest);
    }
    return images.map(({ name }) => ({ name, digest: byName.get(name) }));
}

async function main([command, ...args]) {
    const identity = {
        sourceSha: process.env.SOURCE_COMMIT_SHA,
        runId: process.env.GITHUB_RUN_ID,
        runAttempt: process.env.GITHUB_RUN_ATTEMPT,
    };
    if (command === "matrix" && args.length === 0) return JSON.stringify(createMatrix());
    if (command === "record" && args.length === 2) {
        return JSON.stringify(createReceipt({ name: args[0], digest: args[1], ...identity }));
    }
    if (command === "collect" && args.length === 1) {
        const entries = await readdir(args[0], { withFileTypes: true });
        if (entries.some(entry => !entry.isFile() || !entry.name.endsWith(".json"))) {
            throw new Error("Unexpected entry in image receipt directory.");
        }
        const receipts = await Promise.all(entries.map(async entry =>
            JSON.parse(await readFile(resolve(args[0], entry.name), "utf8"))));
        // Validate everything before writing any output consumed by GitHub Actions.
        return collectReceipts(receipts, identity)
            .map(({ name, digest }) => `${name.replaceAll("-", "_")}=${digest}`).join("\n");
    }
    throw new Error("Usage: deploy-images.mjs matrix | record <image> <digest> | collect <directory>");
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    try {
        process.stdout.write(`${await main(process.argv.slice(2))}\n`);
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
