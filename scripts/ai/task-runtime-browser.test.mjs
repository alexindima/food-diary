import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { join } from "node:path";
import test from "node:test";
import { repositoryRoot } from "./generate-feature.mjs";
import { browserSessionCookie, probeRenderedRuntime } from "./task-runtime-browser.mjs";

const { chromium } = createRequire(
    join(repositoryRoot, "FoodDiary.Web.Client/package.json"),
)("@playwright/test");

const frontendUrl = "http://127.0.0.1:52000";
const expectedUserId = "11111111-1111-4111-8111-111111111111";
const credentials = {
    email: "runtime-probe@example.test",
    password: "synthetic-password-sentinel",
};
const cookieSentinel = "synthetic-refresh-cookie-sentinel";
const unsafeCallLog = `Call log:\n  set-cookie: fooddiary.refresh=${cookieSentinel}; HttpOnly\n  password=${credentials.password}`;

function installFailureProbe(t, respond) {
    const requests = [];
    const diagnostics = [];
    const close = t.mock.fn(async () => {});
    const context = {
        request: {
            post: async (url, options) => {
                requests.push({ url, options });
                return respond();
            },
        },
        addInitScript: () => assert.fail("Failed sign-in must not seed browser storage"),
        newPage: () => assert.fail("Failed sign-in must not open the application"),
    };
    const launch = t.mock.method(chromium, "launch", async () => ({
        newContext: async () => context,
        close,
    }));

    return {
        requests,
        async expectFailure(message) {
            await assert.rejects(
                probeRenderedRuntime(repositoryRoot, frontendUrl, expectedUserId, {
                    credentials,
                    onDiagnostics: events => diagnostics.push(events),
                }),
                error => {
                    assert.equal(error.message, message);
                    assert.equal(error.cause, undefined);
                    assert.ok(!error.stack.includes(cookieSentinel));
                    assert.ok(!error.stack.includes(credentials.password));
                    return true;
                },
            );
            assert.equal(launch.mock.callCount(), 1);
            assert.deepEqual(launch.mock.calls[0].arguments, [{ headless: true }]);
            assert.equal(close.mock.callCount(), 1);
            assert.deepEqual(diagnostics, [[]]);
        },
    };
}

for (const status of [302, 307, 308]) {
    test(`credentialed runtime probe rejects HTTP ${status} without following the redirect`, async t => {
        const probe = installFailureProbe(t, () => ({
            status: () => status,
            json: () => assert.fail("Redirect bodies must not be parsed as account identity"),
        }));

        await probe.expectFailure(`Rendered runtime sign-in failed with status ${status}`);

        assert.equal(probe.requests.length, 1);
        assert.equal(probe.requests[0].url, `${frontendUrl}/api/v1/auth/login`);
        assert.deepEqual(probe.requests[0].options.data, credentials);
        assert.equal(probe.requests[0].options.maxRedirects, 0);
        assert.equal(probe.requests[0].options.timeout, 30_000);
        assert.equal(probe.requests[0].options.headers["X-Requested-With"], "XMLHttpRequest");
    });
}

test("credentialed runtime probe never exposes transport call-log cookies or passwords", async t => {
    const probe = installFailureProbe(t, () => {
        throw new Error(`Response body aborted\n${unsafeCallLog}`);
    });

    await probe.expectFailure("Rendered runtime sign-in request failed");
});

test("credentialed runtime probe never exposes malformed response contents", async t => {
    const probe = installFailureProbe(t, () => ({
        status: () => 200,
        json: async () => {
            throw new SyntaxError(`Unexpected response contents\n${unsafeCallLog}`);
        },
    }));

    await probe.expectFailure("Rendered runtime sign-in returned invalid JSON");
});

test("credentialed runtime probe rejects another account before browser storage is changed", async t => {
    const probe = installFailureProbe(t, () => ({
        status: () => 200,
        json: async () => ({ user: { id: "22222222-2222-4222-8222-222222222222" } }),
    }));

    await probe.expectFailure("Rendered runtime sign-in did not match the owned account");
});

for (const initialConsent of [true, false]) {
    test(`owned dashboard probe completes the real consent gate (required=${initialConsent})`, async t => {
        let consentVisible = initialConsent;
        let checked = false;
        let saved = false;
        const close = t.mock.fn(async () => {});
        const consentResponse = {
            url: () => `${frontendUrl}/api/v1/users/privacy`,
            status: () => 204,
            request: () => ({ method: () => "PUT" }),
        };
        const userResponse = {
            url: () => `${frontendUrl}/api/v1/users/info`,
            status: () => 200,
            json: async () => ({ id: expectedUserId }),
        };
        const consent = {
            isVisible: async () => consentVisible,
            waitFor: async ({ state }) => {
                assert.equal(state, "hidden");
                assert.equal(consentVisible, false);
                assert.equal(saved, true);
            },
            locator: selector => {
                if (selector === "#health-consent-agreement")
                    return { check: async () => { checked = true; } };
                assert.equal(selector, "fd-ui-button");
                return { last: () => ({ getByRole: role => {
                    assert.equal(role, "button");
                    return { click: async () => {
                        assert.equal(checked, true);
                        saved = true;
                        consentVisible = false;
                    } };
                } }) };
            },
        };
        const page = {
            on: () => {},
            waitForResponse: async predicate => {
                if (predicate(userResponse)) return userResponse;
                assert.equal(predicate(consentResponse), true);
                assert.equal(predicate({ ...consentResponse, url: () => "https://other.example/api/v1/users/privacy" }), false);
                return consentResponse;
            },
            goto: async () => {},
            locator: selector => {
                if (selector === "fd-health-consent-dialog") return consent;
                if (selector === "fd-dashboard, fd-health-consent-dialog")
                    return { first: () => ({ waitFor: async () => {} }) };
                if (selector === "fd-dashboard") return { waitFor: async () => {
                    assert.equal(consentVisible, false, "Dashboard remains gated until consent is saved");
                } };
                assert.equal(selector, "body");
                return { waitFor: async () => {}, innerText: async () => "Synthetic dashboard successfully rendered" };
            },
            waitForLoadState: async () => {},
            url: () => `${frontendUrl}/dashboard`,
            title: async () => "Dashboard",
        };
        t.mock.method(chromium, "launch", async () => ({
            newContext: async () => ({
                request: { post: async () => ({ status: () => 200, json: async () => ({ user: { id: expectedUserId } }) }) },
                newPage: async () => page,
            }),
            close,
        }));
        const result = await probeRenderedRuntime(repositoryRoot, frontendUrl, expectedUserId, { credentials });
        assert.equal(result.ownedIdentityMatches, true);
        assert.equal(saved, initialConsent);
        assert.equal(close.mock.callCount(), 1);
    });
}
