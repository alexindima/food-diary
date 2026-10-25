import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { createServer } from "node:http";
import {
    inspectPlatformPage,
    platformBrowserChecks,
} from "../../FoodDiary.Web.Client/quality/modern-web-guidance/browser-platform.mjs";
const require = createRequire(
    new URL("../../FoodDiary.Web.Client/package.json", import.meta.url),
);
const { chromium } = require("@playwright/test");
const fixture = (
    bad,
) => `<!doctype html><html lang="en"><head><title>Platform assertions</title><style>
body {font: 1rem/1.5 system-ui; margin: 1rem} main {max-width:100%} button,input {font:inherit;max-width:100%;box-sizing:border-box} dialog {max-width:calc(100% - 3rem)}label {display:block} input {width:100%}
.moving {width:10px;height:10px;background:blue} @keyframes shift {to {transform:translateX(5px)}}
${bad ? ":focus-visible {outline:none !important; box-shadow:none !important}" : ":focus-visible {outline:2px solid navy;outline-offset:2px}"}
${bad ? ".moving {animation:shift 10s infinite}.wide{width:1800px;height:2px}" : "@media(prefers-reduced-motion:no-preference){.moving{animation:shift 10s infinite}}"}
</style></head><body>${bad ? "" : '<a href="#main">Skip to main</a>'}<main id="main" tabindex="-1"><h1>Platform fixture</h1><${bad ? "h3" : "h2"}>Detail</${bad ? "h3" : "h2"}><p>Readable content and accessible controls remain available when text grows.</p><button type="button">Action</button><div class="wide"></div><div class="moving"></div>${bad ? '<span id="duplicate"></span><span id="duplicate"></span><img alt="" fetchpriority="high"><img alt="" fetchpriority="high"><img alt="" fetchpriority="high">' : ""}</main>
<dialog aria-label="Sign in"><form method="${bad ? "get" : "post"}" novalidate><label for="email">Email</label><input id="email" name="email" type="email" autocomplete="${bad ? "off" : "username"}" spellcheck="${bad}" required aria-describedby="email-error"><p id="email-error"></p><label for="current-password">Password</label><input id="current-password" name="current-password" type="password" autocomplete="current-password" required aria-describedby="password-error"><p id="password-error"></p><button type="submit">Sign in</button></form></dialog>
<script>document.querySelector('form').addEventListener('submit',event=>{event.preventDefault();${bad ? "" : `const fields=[...event.target.querySelectorAll('input')];for(const field of fields){field.setAttribute('aria-invalid',String(!field.checkValidity()));document.getElementById(field.getAttribute('aria-describedby')).textContent=field.checkValidity()?'':'Required';}fields.find(field=>!field.checkValidity())?.focus();`}});</script></body></html>`;
const server = createServer((request, response) => {
    response.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    let html = fixture(request.url === "/bad");
    if (request.url.startsWith("/nested")) {
        html = html.replace("<head>", '<head><base href="/">');
        if (!request.url.includes("broken"))
            html = html.replace(
                'href="#main"',
                'href="/nested?state=preserved#main"',
            );
    }
    if (request.url.startsWith("/table-"))
        html = html.replace(
            "</main>",
            `<div role="region" aria-label="Data" tabindex="0" style="max-width:100%;overflow-x:${request.url === "/table-good" ? "auto" : "hidden"}"><table style="min-width:1000px"><tr><td style="min-width:900px">Data</td><td><button type="button">Edit</button></td></tr></table></div></main>`,
        );
    response.end(html);
});
await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const browser = await chromium.launch({
    headless: true,
    channel: process.env.FD_WEB_QUALITY_CHANNEL ?? "chrome",
});
try {
    const context = await browser.newContext({
        viewport: { width: 390, height: 844 },
        reducedMotion: "reduce",
    });
    const page = await context.newPage();
    page.setDefaultTimeout(5000);
    for (const check of platformBrowserChecks) {
        for (const bad of [false, true]) {
            await page.goto(
                `http://127.0.0.1:${server.address().port}/${bad ? "bad" : "good"}`,
            );
            if (check.startsWith("browser:form-"))
                await page
                    .locator("dialog")
                    .evaluate((element) => element.showModal());
            const result = await inspectPlatformPage(page, check, {
                kind: "login",
            });
            assert.ok(
                result.observations > 0,
                `${check}: actual observations required`,
            );
            assert.equal(
                result.findings.length > 0,
                bad,
                `${check} ${bad ? "invalid" : "valid"}: ${JSON.stringify(result.findings)}`,
            );
        }
        console.log(`Verified valid/invalid browser fixtures: ${check}`);
    }
    await page.goto(`http://127.0.0.1:${server.address().port}/good`);
    await page.locator("dialog").evaluate((element) => {
        element.showModal();
        for (const input of element.querySelectorAll("input"))
            Object.defineProperty(input, "autocomplete", { get: () => "" });
    });
    assert.equal(
        (
            await inspectPlatformPage(page, "browser:form-attributes", {
                kind: "login",
            })
        ).findings.length,
        0,
        "HTML credential tokens remain valid without DOM property reflection.",
    );
    for (const broken of [false, true]) {
        await page.goto(
            `http://127.0.0.1:${server.address().port}/nested?state=${broken ? "broken" : "preserved"}`,
        );
        const result = await inspectPlatformPage(page, "browser:skip-link", {});
        assert.equal(
            result.findings.length > 0,
            broken,
            "Resolve skip links against base href and retain route/query",
        );
        assert.equal(
            new URL(page.url()).pathname,
            "/nested",
            "A broken skip link must be diagnosed before navigating away",
        );
    }
    for (const broken of [false, true]) {
        await page.goto(
            `http://127.0.0.1:${server.address().port}/table-${broken ? "bad" : "good"}`,
        );
        const result = await inspectPlatformPage(
            page,
            "browser:narrow-reflow",
            {},
        );
        assert.equal(
            result.findings.length > 0,
            broken,
            "Only bounded keyboard-scrollable data tables qualify for the 2D reflow exception",
        );
    }
    await context.close();
} finally {
    await browser.close();
    await new Promise((resolve) => server.close(resolve));
}
