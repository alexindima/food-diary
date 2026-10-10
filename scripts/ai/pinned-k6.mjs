import { createHash } from "node:crypto";
import { spawnSync } from "node:child_process";
import {
    chmodSync,
    existsSync,
    mkdirSync,
    readFileSync,
    writeFileSync,
} from "node:fs";
import { join } from "node:path";
import { repositoryRoot } from "./generate-feature.mjs";

export async function pinnedK6(root = repositoryRoot) {
    if (
        process.arch !== "x64" ||
        !["win32", "linux"].includes(process.platform)
    )
        throw new Error("Pinned k6 bootstrap supports Windows/Linux x64");
    const windows = process.platform === "win32";
    const name = `k6-v1.3.0-${windows ? "windows-amd64.zip" : "linux-amd64.tar.gz"}`;
    const expected = windows
        ? "06a15d69869efb9261e6c132af64078731ad7d048095eaca05198daacb17686d"
        : "84d26fc1f7bc03e02f2e016b3b1b20c032e05dfe461fca82de4e3a6ebe72ddbd";
    const directory = join(
        root,
        ".artifacts/tool-cache/k6",
        `v1.3.0-${process.platform}-x64`,
    );
    mkdirSync(directory, { recursive: true });
    const executable = join(directory, windows ? "k6.exe" : "k6");
    const receipt = join(directory, "verified.json");
    const hash = (bytes) => createHash("sha256").update(bytes).digest("hex");
    if (existsSync(executable) && existsSync(receipt)) {
        const proof = JSON.parse(readFileSync(receipt, "utf8"));
        if (
            proof.archiveSha256 === expected &&
            proof.binarySha256 === hash(readFileSync(executable))
        )
            return executable;
        throw new Error("Cached k6 differs from its verification receipt");
    }
    const response = await fetch(
        `https://github.com/grafana/k6/releases/download/v1.3.0/${name}`,
        { signal: AbortSignal.timeout(120_000) },
    );
    if (!response.ok)
        throw new Error("Pinned k6 release could not be downloaded");
    const bytes = Buffer.from(await response.arrayBuffer());
    if (bytes.length > 100 * 1024 * 1024 || hash(bytes) !== expected)
        throw new Error(
            "Pinned k6 archive checksum differs from the official release",
        );
    const archive = join(directory, name);
    writeFileSync(archive, bytes);
    const extract = windows
        ? spawnSync(
              "pwsh",
              [
                  "-NoProfile",
                  "-File",
                  join(root, "scripts/ai/Extract-OwnedK6.ps1"),
                  "-ArchivePath",
                  archive,
                  "-DestinationPath",
                  directory,
              ],
              { windowsHide: true, encoding: "utf8" },
          )
        : spawnSync(
              "tar",
              [
                  "-xzf",
                  archive,
                  "-C",
                  directory,
                  "--strip-components=1",
                  "k6-v1.3.0-linux-amd64/k6",
              ],
              { encoding: "utf8" },
          );
    if (extract.error || extract.status !== 0 || !existsSync(executable))
        throw new Error("Pinned k6 extraction failed");
    if (!windows) chmodSync(executable, 0o700);
    writeFileSync(
        receipt,
        JSON.stringify({
            version: "1.3.0",
            archiveSha256: expected,
            binarySha256: hash(readFileSync(executable)),
            source: "official grafana/k6 v1.3.0 release asset",
        }),
    );
    return executable;
}
