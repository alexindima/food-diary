import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const workflow = await readFile(
    new URL("../workflows/deploy.yml", import.meta.url),
    "utf8",
);
const expression = workflow
    .match(/    if: >-\r?\n([\s\S]*?)    runs-on:/)?.[1]
    .trim();
assert.ok(expression, "Deploy must authorize the event at job level");
const allows = new Function("github", `return (${expression});`);
const repository = "alexindima/food-diary";
const push = {
    conclusion: "success",
    event: "push",
    head_branch: "master",
    head_repository: { full_name: repository },
};

for (const [name, event, ref, run, expected] of [
    ["master push", "workflow_run", "refs/heads/master", push, true],
    [
        "fork PR named master",
        "workflow_run",
        "refs/heads/master",
        {
            ...push,
            event: "pull_request",
            head_repository: { full_name: "attacker/fork" },
        },
        false,
    ],
    [
        "same repository PR",
        "workflow_run",
        "refs/heads/master",
        { ...push, event: "pull_request" },
        false,
    ],
    [
        "foreign push",
        "workflow_run",
        "refs/heads/master",
        { ...push, head_repository: { full_name: "attacker/fork" } },
        false,
    ],
    [
        "other branch",
        "workflow_run",
        "refs/heads/master",
        { ...push, head_branch: "feature" },
        false,
    ],
    [
        "failed CI",
        "workflow_run",
        "refs/heads/master",
        { ...push, conclusion: "failure" },
        false,
    ],
    [
        "manual master",
        "workflow_dispatch",
        "refs/heads/master",
        undefined,
        true,
    ],
    [
        "manual feature",
        "workflow_dispatch",
        "refs/heads/feature",
        undefined,
        false,
    ],
]) {
    test(name, () =>
        assert.equal(
            allows({
                repository,
                event_name: event,
                ref,
                event: { workflow_run: run },
            }),
            expected,
        ),
    );
}

test("source authorization runs before checkout, credentials and source scripts", () => {
    const guard = workflow.indexOf("- name: Authorize deployment source");
    assert.ok(guard > 0 && guard < workflow.indexOf("- name: Checkout code"));
    assert.ok(
        workflow.includes(
            '"${GITHUB_API_URL}/repos/${GITHUB_REPOSITORY}/git/ref/heads/master"',
        ),
    );
    assert.ok(workflow.includes('"${SOURCE_COMMIT_SHA}" != "${master_sha}"'));
    assert.ok(workflow.includes("curl --fail --silent --show-error"));
});
