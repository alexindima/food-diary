import test from "node:test";
import assert from "node:assert/strict";
import { snapshotDom } from "../../../FoodDiary.Web.Client/quality/modern-web-guidance/browser.mjs";

function frame(url, evaluate = async () => ["<html>Complete document</html>"]) {
    return {
        url: () => url,
        evaluate,
        waitForLoadState: async () => {},
        isDetached: () => false,
    };
}

function page(main, children = []) {
    return { mainFrame: () => main, frames: () => [main, ...children] };
}

const navigationError = () =>
    new Error(
        "frame.evaluate: Execution context was destroyed, most likely because of a navigation",
    );

test("a navigating child invalidates the whole DOM snapshot", async () => {
    const main = frame("https://fixture.test/");
    const child = frame("https://provider.test/", async () => {
        throw navigationError();
    });
    assert.equal(await snapshotDom(page(main, [child])), null);
});

test("a detached child invalidates the snapshot instead of omitting its document", async () => {
    const main = frame("https://fixture.test/");
    const child = frame("https://provider.test/", async () => {
        throw new Error("Frame was detached");
    });
    child.isDetached = () => true;
    assert.equal(await snapshotDom(page(main, [child])), null);
});

test("the next complete snapshot includes the settled child document", async () => {
    const main = frame("https://fixture.test/");
    let navigating = true;
    const child = frame("https://provider.test/", async () => {
        if (navigating) throw navigationError();
        return [
            "<html>Settled child</html>",
            "<button>Shadow control</button>",
        ];
    });
    const document = page(main, [child]);
    assert.equal(await snapshotDom(document), null);
    navigating = false;
    assert.deepEqual(JSON.parse(await snapshotDom(document)), [
        { url: main.url(), roots: ["<html>Complete document</html>"] },
        {
            url: child.url(),
            roots: [
                "<html>Settled child</html>",
                "<button>Shadow control</button>",
            ],
        },
    ]);
});

test("main document navigation remains an audit error", async () => {
    const main = frame("https://fixture.test/", async () => {
        throw navigationError();
    });
    await assert.rejects(
        snapshotDom(page(main)),
        /Execution context was destroyed/u,
    );
});

test("unrelated child evaluation failures remain audit errors", async () => {
    const main = frame("https://fixture.test/");
    const child = frame("https://provider.test/", async () => {
        throw new Error("Unexpected DOM failure");
    });
    await assert.rejects(
        snapshotDom(page(main, [child])),
        /Unexpected DOM failure/u,
    );
});

test("a newly attached child invalidates the snapshot", async () => {
    const children = [];
    const main = frame("https://fixture.test/", async () => {
        children.push(frame("https://provider.test/"));
        return ["<html>Document with new iframe</html>"];
    });
    assert.equal(await snapshotDom(page(main, children)), null);
});

test("a child URL change during capture invalidates the snapshot", async () => {
    const main = frame("https://fixture.test/");
    let url = "about:blank";
    const child = frame(url, async () => {
        url = "https://provider.test/";
        return ["<html>New document</html>"];
    });
    child.url = () => url;
    assert.equal(await snapshotDom(page(main, [child])), null);
});
