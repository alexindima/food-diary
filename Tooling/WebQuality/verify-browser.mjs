import assert from "node:assert/strict";
import { createServer } from "node:http";
import { mkdir } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { readJson } from "./lib/files.mjs";
import { initialResults } from "./lib/policy.mjs";
import { coverage, renderReport } from "./lib/report.mjs";
import { runBrowser } from "../../FoodDiary.Web.Client/quality/modern-web-guidance/browser.mjs";

// An executable integration check for the checker, independent of app state or credentials.
const root = path.resolve(import.meta.dirname, "../..");
const output = path.join(root, ".artifacts/web-quality/browser-contracts");
await mkdir(output, { recursive: true });
const policy = await readJson(path.join(import.meta.dirname, "policy.json"));
const lock = await readJson(
    path.join(import.meta.dirname, "upstream/upstream.lock.json"),
);
const report = {
    upstream: lock,
    catalogue: coverage(lock, policy),
    results: initialResults(policy),
    provenance: { revision: "controlled-fixture", dirty: false },
    executions: {},
    startedAt: new Date().toISOString(),
};
report.results[0] = {
    ...report.results[0],
    status: "fail",
    observations: 1,
    findings: [
        {
            file: "fixture.html",
            message: "Controlled missing alternative text.",
        },
    ],
};
const reportHtml = renderReport(report, policy);
const fixture = (bad) =>
    `<!doctype html><html lang="${bad ? "fr" : "en"}"><head><meta charset="utf-8"><title>${bad ? "" : "Quality fixture"}</title><meta name="viewport" content="width=device-width,initial-scale=1"><style>button{min-width:44px;min-height:44px}body{font-family:system-ui;${bad ? "min-width:2400px" : ""}}dialog{padding:2rem}label{display:block}</style></head><body><main><h1>Browser verification fixture</h1><p>This controlled document checks real browser assertions and modal keyboard behavior.</p><button id="open" type="button">Sign in</button></main><dialog aria-label="Account access"><form method="dialog"><label>Email<input type="email" autocomplete="username"></label><label>Password<input type="password" autocomplete="current-password"></label><button type="submit">Close</button></form></dialog><script>document.getElementById('open').onclick=()=>document.querySelector('dialog').showModal()</script></body></html>`;
const stylesheetServer = createServer((request, response) => {
    if (request.url === "/frame-error") {
        response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
        response.end(
            '<!doctype html><html lang="en"><title>Frame fixture</title><body><main><h1>Frame fixture</h1></main><script>throw new Error("Controlled frame exception")</script></body></html>',
        );
        return;
    }
    // Stylesheet inclusion is allowed; axe's CSSOM XHR gets no CORS permission.
    response.writeHead(200, { "content-type": "text/css; charset=utf-8" });
    response.end("body{color:#111;background:#fff}");
});
await new Promise((resolve) =>
    stylesheetServer.listen(0, "127.0.0.1", resolve),
);
const stylesheetOrigin = `http://127.0.0.1:${stylesheetServer.address().port}`;
const server = createServer((request, response) => {
    response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    if (request.url === "/deferred-frame-good") {
        response.end(
            fixture(false).replace(
                "</main>",
                '<iframe title="Deferred child" src="/deferred-good"></iframe></main>',
            ),
        );
        return;
    }
    if (request.url.startsWith("/deferred-")) {
        const mode = request.url.slice("/deferred-".length);
        response.end(
            fixture(false)
                .replace(
                    "</main>",
                    `${mode === "added-bad" ? "" : mode === "shadow-good" ? '<div id="shadow-host"></div>' : '<button id="deferred" type="button" style="width:44px;height:44px"></button>'}</main>`,
                )
                .replace(
                    "</body>",
                    `<script>
                    let assignedAxe;
                    let renderStep = 0;
                    const shadowHost = document.getElementById('shadow-host');
                    if (shadowHost) shadowHost.attachShadow({mode: 'open'}).innerHTML = '<button id="deferred" type="button" style="width:44px;height:44px"></button>';
                    // axe publishes window.axe before assigning its API methods.
                    // Intercept runPartial's assignment to keep the real scan and
                    // deterministically finish rendering immediately after it.
                    Object.defineProperty(window, 'axe', {
                        configurable: true,
                        get: () => assignedAxe,
                        set(value) {
                            assignedAxe = value;
                            let wrappedRunPartial;
                            Object.defineProperty(value, 'runPartial', {
                                configurable: true,
                                get: () => wrappedRunPartial,
                                set(runPartial) {
                                    wrappedRunPartial = async function(...args) {
                                        const mode = ${JSON.stringify(mode)};
                                        if (mode === 'error') throw new Error('Controlled axe failure');
                                        const result = await runPartial.apply(this, args);
                                        let button = (shadowHost?.shadowRoot ?? document).getElementById('deferred');
                                        if (mode === 'added-bad' && !button) {
                                            button = document.createElement('button');
                                            button.id = 'deferred';
                                            button.type = 'button';
                                            button.style.cssText = 'width:44px;height:44px';
                                            document.querySelector('main').append(button);
                                        } else if (mode === 'changing') {
                                            button.setAttribute('aria-label', 'Loaded action');
                                            button.dataset.renderStep = String(++renderStep);
                                        } else if (button && !button.dataset.rendered) {
                                            button.dataset.rendered = 'true';
                                            if (mode === 'good' || mode === 'shadow-good') button.setAttribute('aria-label', 'Loaded action');
                                        }
                                        return result;
                                    };
                                }
                            });
                        }
                    });
                    </script></body>`,
                ),
        );
        return;
    }
    const runtimeScripts = {
        "/stylesheet-probe": "",
        "/handled-rejection":
            'Promise.reject(new Error("Controlled handled rejection")).catch(()=>{})',
        "/uncaught-exception":
            'throw new Error("Controlled runtime exception")',
        "/unhandled-rejection":
            'Promise.reject("Controlled runtime rejection")',
        "/frame-exception": "",
    };
    if (Object.hasOwn(runtimeScripts, request.url)) {
        response.end(
            fixture(false)
                .replace(
                    "</head>",
                    `<link rel="stylesheet" href="${stylesheetOrigin}/style.css"></head>`,
                )
                .replace(
                    "</body>",
                    `${request.url === "/frame-exception" ? `<iframe title="Runtime fixture" src="${stylesheetOrigin}/frame-error"></iframe>` : ""}<script>${runtimeScripts[request.url]}</script></body>`,
                ),
        );
        return;
    }
    if (request.url === "/client-redirect") {
        response.end(
            fixture(false).replace(
                "</body>",
                `<script>setTimeout(()=>{history.replaceState({},'', '/good');document.body.id='redirected-ready'},500)</script></body>`,
            ),
        );
        return;
    }
    if (request.url === "/attribute-reflection") {
        response.end(
            fixture(false).replace(
                "<script>",
                '<script>for(const input of document.querySelectorAll("input"))Object.defineProperty(input,"autocomplete",{get:()=>""});',
            ),
        );
        return;
    }
    if (request.url === "/broken-credentials") {
        response.end(
            fixture(false)
                .replaceAll('autocomplete="username"', 'autocomplete="off"')
                .replaceAll(
                    'autocomplete="current-password"',
                    'autocomplete="off"',
                ),
        );
        return;
    }
    if (request.url === "/escaped-modal") {
        response.end(
            fixture(false)
                .replace(
                    '<dialog aria-label="Account access">',
                    '<div role="dialog" aria-label="Account access" hidden>',
                )
                .replace("</dialog>", "</div>")
                .replace(
                    "document.getElementById('open').onclick=()=>document.querySelector('dialog').showModal()",
                    "document.getElementById('open').onclick=()=>{const root=document.querySelector('[role=dialog]');root.hidden=false;root.querySelector('input').focus()};document.addEventListener('keydown',event=>{if(event.key==='Escape'){document.querySelector('[role=dialog]').hidden=true;document.getElementById('open').focus()}})",
                ),
        );
        return;
    }
    response.end(
        request.url === "/report"
            ? reportHtml
            : fixture(request.url === "/bad"),
    );
});
await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const origin = `http://127.0.0.1:${server.address().port}`;
const selected = [
    "page-title",
    "document-language",
    "horizontal-overflow",
    "login-autocomplete",
    "dialog-focus",
];
const controlledPolicy = {
    ...policy,
    requirements: policy.requirements.filter((item) =>
        selected.includes(item.id),
    ),
};
const scenario = {
    id: "controlled-modal",
    kind: "login",
    scope: "client",
    path: "/good",
    locale: "en",
    viewport: { width: 390, height: 844 },
    readySelector: "main",
    openDialog: "Sign in",
};
try {
    const runtimePolicy = {
        ...policy,
        requirements: policy.requirements.filter((item) =>
            ["axe", "page-title"].includes(item.id),
        ),
    };
    for (const browserName of ["chromium", "firefox", "webkit"]) {
        const deferred = await runBrowser({
            policy: runtimePolicy,
            baseURL: origin,
            browserName,
            scenarios: [
                "good",
                "bad",
                "added-bad",
                "changing",
                "error",
                "shadow-good",
                "frame-good",
            ].map((mode) => ({
                ...scenario,
                id: `deferred-${mode}`,
                path: `/deferred-${mode}`,
                kind: "public",
                openDialog: undefined,
            })),
            artifactDirectory: path.join(output, `deferred-${browserName}`),
        });
        const axe = deferred.results.find((item) => item.requirement === "axe");
        for (const id of [
            "deferred-good",
            "deferred-shadow-good",
            "deferred-frame-good",
        ]) {
            const goodCase = axe.cases.find((item) => item.id === id);
            assert.ok(
                goodCase &&
                    !goodCase.error &&
                    goodCase.observations > 0 &&
                    !axe.findings.some((item) => item.case === id),
                `${browserName}: axe must inspect the completed deferred render (${id}): ${JSON.stringify(axe)}`,
            );
            assert.match(goodCase.detail, /stable DOM after 2 attempts/u);
        }
        for (const mode of ["bad", "added-bad"]) {
            assert.ok(
                axe.findings.some(
                    (item) =>
                        item.case === `deferred-${mode}` &&
                        item.message.startsWith("button-name:"),
                ),
                `${browserName}: stable ${mode} controls must still fail button-name`,
            );
        }
        assert.match(
            axe.cases.find((item) => item.id === "deferred-changing")?.error ??
                "",
            /DOM kept changing during axe audit \(3 attempts\)/u,
            `${browserName}: continuously changing DOM must fail within the attempt limit`,
        );
        assert.match(
            axe.cases.find((item) => item.id === "deferred-error")?.error ?? "",
            /Controlled axe failure/u,
            `${browserName}: axe execution errors must not be retried or suppressed`,
        );
        const runtime = await runBrowser({
            policy: runtimePolicy,
            baseURL: origin,
            browserName,
            scenarios: [
                "stylesheet-probe",
                "handled-rejection",
                "uncaught-exception",
                "unhandled-rejection",
                "frame-exception",
            ].map((id) => ({
                ...scenario,
                id,
                path: `/${id}`,
                kind: "public",
                openDialog: undefined,
            })),
            artifactDirectory: path.join(output, `runtime-${browserName}`),
        });
        for (const result of runtime.results) {
            for (const id of ["stylesheet-probe", "handled-rejection"]) {
                const observed = result.cases.find((item) => item.id === id);
                assert.ok(
                    observed && !observed.error && observed.observations > 0,
                    `${browserName} handled CSSOM/rejection diagnostics must preserve valid ${result.requirement} evidence: ${observed?.error}`,
                );
            }
            for (const [id, message] of [
                ["uncaught-exception", "Controlled runtime exception"],
                ["unhandled-rejection", "Controlled runtime rejection"],
                ["frame-exception", "Controlled frame exception"],
            ]) {
                assert.ok(
                    result.cases
                        .find((item) => item.id === id)
                        ?.error?.includes(message),
                    `${browserName} must reject ${id} in ${result.requirement} evidence`,
                );
            }
        }
    }
    const good = await runBrowser({
        policy: controlledPolicy,
        baseURL: origin,
        scenarios: [scenario],
        artifactDirectory: path.join(output, "good"),
    });
    assert.ok(
        good.results.every((item) => item.status === "pass"),
        JSON.stringify(good.results),
    );
    const reflected = await runBrowser({
        policy: controlledPolicy,
        baseURL: origin,
        scenarios: [
            {
                ...scenario,
                id: "attribute-reflection",
                path: "/attribute-reflection",
            },
        ],
        artifactDirectory: path.join(output, "attribute-reflection"),
    });
    assert.ok(
        reflected.results.every((item) => item.status === "pass"),
        "Valid HTML tokens do not depend on optional DOM property reflection.",
    );
    for (const [route, requirement] of [
        ["broken-credentials", "login-autocomplete"],
        ["escaped-modal", "dialog-focus"],
    ]) {
        const broken = await runBrowser({
            policy: {
                ...controlledPolicy,
                requirements: controlledPolicy.requirements.filter(
                    (item) => item.id === requirement,
                ),
            },
            baseURL: origin,
            scenarios: [{ ...scenario, id: route, path: `/${route}` }],
            artifactDirectory: path.join(output, route),
        });
        assert.equal(
            broken.results[0].status,
            "fail",
            `${requirement} must reject a real invalid fixture: ${JSON.stringify(broken.results)}`,
        );
    }
    const badPolicy = {
        ...controlledPolicy,
        requirements: controlledPolicy.requirements.filter(
            (item) => !["login-autocomplete", "dialog-focus"].includes(item.id),
        ),
    };
    const bad = await runBrowser({
        policy: badPolicy,
        baseURL: origin,
        scenarios: [
            {
                ...scenario,
                id: "controlled-bad",
                path: "/bad",
                kind: "public",
                openDialog: undefined,
            },
        ],
        artifactDirectory: path.join(output, "bad"),
    });
    assert.ok(
        bad.results.every((item) => item.status === "fail"),
        JSON.stringify(bad.results),
    );
    const redirected = await runBrowser({
        policy: badPolicy,
        baseURL: origin,
        scenarios: [
            {
                ...scenario,
                id: "controlled-redirect",
                path: "/client-redirect",
                kind: "public",
                openDialog: undefined,
                readySelector: "#redirected-ready main",
            },
        ],
        artifactDirectory: path.join(output, "redirected"),
    });
    assert.ok(
        redirected.results.every((item) => item.status === "error"),
        "Client redirects during readiness must invalidate route evidence.",
    );
    const missing = await runBrowser({
        policy: badPolicy,
        baseURL: origin,
        scenarios: [
            {
                ...scenario,
                id: "controlled-missing",
                readySelector: "#never-ready",
            },
        ],
        artifactDirectory: path.join(output, "missing"),
    });
    assert.ok(missing.results.every((item) => item.status === "error"));
    const require = createRequire(
        new URL("../../FoodDiary.Web.Client/package.json", import.meta.url),
    );
    const module = await import(
        pathToFileURL(require.resolve("@playwright/test")).href
    );
    const browser = await (module.default ?? module).chromium.launch({
        channel: process.env.FD_WEB_QUALITY_CHANNEL ?? "chrome",
        headless: true,
    });
    try {
        const page = await browser.newPage({
            viewport: { width: 1440, height: 1000 },
        });
        await page.goto(`${origin}/report`);
        assert.equal(
            await page.locator(".requirement:visible").count(),
            policy.requirements.length,
        );
        await page.locator("#status").selectOption("fail");
        assert.equal(await page.locator(".requirement:visible").count(), 1);
        await page.locator("#search").fill("does-not-exist");
        assert.equal(await page.locator(".requirement:visible").count(), 0);
        await page.locator("#search").fill("");
        await page.locator("#status").selectOption("");
        await page.screenshot({
            path: path.join(output, "report-desktop.png"),
        });
        await page.setViewportSize({ width: 390, height: 844 });
        assert.ok(
            await page.evaluate(
                () => document.documentElement.scrollWidth <= innerWidth + 2,
            ),
        );
        await page.screenshot({ path: path.join(output, "report-mobile.png") });
    } finally {
        await browser.close();
    }
    console.log(
        "Verified real browser assertions, rejected invalid/empty evidence, modal focus boundaries, report filters and desktop/mobile layout.",
    );
} finally {
    await new Promise((resolve) => server.close(resolve));
    await new Promise((resolve) => stylesheetServer.close(resolve));
}
