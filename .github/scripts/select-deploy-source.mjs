import { execFileSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";

const fullSha = /^[0-9a-f]{40}$/u;

function normalizeSha(value) {
    const sha = value.trim().toLowerCase();
    if (sha && !fullSha.test(sha)) throw new Error("commit_sha must be a full 40-character commit SHA.");
    return sha;
}

export function selectDeploymentSource(requestedSha, masterHistory, runs, repository) {
    const target = normalizeSha(requestedSha);
    if (target && !masterHistory.includes(target)) {
        throw new Error("Requested commit is not in master history.");
    }

    const passed = new Map();
    for (const run of runs) {
        if (run.event === "push" && run.head_branch === "master" &&
            run.head_repository?.full_name === repository &&
            run.status === "completed" && run.conclusion === "success" &&
            fullSha.test(run.head_sha) && Number.isSafeInteger(run.id) && run.id > 0) {
            passed.set(run.head_sha, run.id);
        }
    }

    const sha = target || masterHistory.find(commit => passed.has(commit));
    if (!sha || !passed.has(sha)) {
        throw new Error(target
            ? "Requested commit has no successful CI Tests push run on master."
            : "No commit in master history has a successful CI Tests push run.");
    }
    return { sha, ciRunId: passed.get(sha) };
}

function readRuns(repository, target) {
    const args = [
        "api", "--method", "GET", `repos/${repository}/actions/workflows/ci-tests.yml/runs`,
        "-f", "branch=master", "-f", "event=push", "-f", "status=success", "-f", "per_page=100",
        "--jq", ".workflow_runs[] | {id, head_sha, head_branch, head_repository: {full_name: .head_repository.full_name}, event, status, conclusion}",
        "--paginate",
    ];
    if (target) args.push("-f", `head_sha=${target}`);
    const output = execFileSync("gh", args, { encoding: "utf8" }).trim();
    return output ? output.split(/\r?\n/u).map(line => JSON.parse(line)) : [];
}

function main() {
    const target = normalizeSha(process.env.REQUESTED_SHA ?? "");
    const repository = process.env.GITHUB_REPOSITORY;
    // Use the order in Git history so a rerun for an older commit cannot take precedence.
    const masterHistory = execFileSync("git", ["rev-list", "--date-order", "refs/remotes/origin/master"],
        { encoding: "utf8" }).trim().split(/\r?\n/u);
    const runs = readRuns(repository, target);
    const { sha, ciRunId } = selectDeploymentSource(target, masterHistory, runs, repository);
    const ciUrl = `${process.env.GITHUB_SERVER_URL}/${repository}/actions/runs/${ciRunId}`;
    appendFileSync(process.env.GITHUB_OUTPUT, `source_sha=${sha}\n`);
    appendFileSync(process.env.GITHUB_STEP_SUMMARY,
        `- Deployment commit: \`${sha}\`\n- Successful [CI Tests run](${ciUrl})\n`);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    try {
        main();
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
