import { createRequire } from "node:module";
import { join } from "node:path";
import { redactDiagnostic } from "./diagnostic-redaction.mjs";

export async function probeRenderedRuntime(
    root,
    frontendUrl,
    accessToken,
    expectedUserId,
    { onDiagnostics = () => {}, probeFailure = false } = {},
) {
    const { chromium } = createRequire(
        join(root, "FoodDiary.Web.Client/package.json"),
    )("@playwright/test");
    const browser = await chromium.launch({ headless: true });
    const events = [];
    const record = (event) => {
        if (events.length < 40)
            events.push({ atUtc: new Date().toISOString(), ...event });
    };
    try {
        const context = await browser.newContext();
        await context.addInitScript(
            (token) => localStorage.setItem("authToken", token),
            accessToken,
        );
        const page = await context.newPage();
        page.on("pageerror", (error) =>
            record({
                kind: "browser-error",
                message: redactDiagnostic(error.message),
                location: redactDiagnostic(error.stack ?? "", 600),
            }),
        );
        page.on("console", (message) => {
            if (message.type() === "error") {
                const location = message.location();
                record({
                    kind: "console-error",
                    message: redactDiagnostic(message.text()),
                    location: {
                        ...location,
                        url: redactDiagnostic(location.url),
                    },
                });
            }
        });
        page.on("response", (response) => {
            const url = new URL(response.url());
            if (url.origin === frontendUrl && response.status() >= 400) {
                const headers = response.headers();
                const correlation =
                    headers["x-correlation-id"] ?? headers["traceparent"];
                record({
                    kind: "http-error",
                    method: response.request().method(),
                    path: url.pathname,
                    status: response.status(),
                    correlationId:
                        correlation && /^[A-Za-z0-9-]{1,80}$/u.test(correlation)
                            ? correlation
                            : null,
                });
            }
        });
        page.on("requestfailed", (request) => {
            const url = new URL(request.url());
            if (url.origin === frontendUrl)
                record({
                    kind: "network-error",
                    method: request.method(),
                    path: url.pathname,
                    message: redactDiagnostic(
                        request.failure()?.errorText ?? "Request failed",
                    ),
                });
        });
        const userResponse = page.waitForResponse(
            (response) =>
                new URL(response.url()).pathname === "/api/v1/users/info" &&
                response.status() === 200,
            { timeout: 45_000 },
        );
        await page.goto(`${frontendUrl}/dashboard`, {
            waitUntil: "domcontentloaded",
            timeout: 60_000,
        });
        const response = await userResponse;
        const user = await response.json();
        if (
            user.id !== expectedUserId ||
            new URL(response.url()).origin !== frontendUrl
        )
            throw new Error(
                "Rendered app did not reach its owned API through the task proxy",
            );
        await page
            .locator("fd-dashboard")
            .waitFor({ state: "visible", timeout: 20_000 });
        try {
            await page.waitForLoadState("networkidle", { timeout: 10_000 });
        } catch {
            record({
                kind: "observation-incomplete",
                message:
                    "Background network activity did not settle within the observation window",
            });
        }
        if (probeFailure)
            await page.evaluate(async () => {
                await fetch("/api/v1/agent-diagnostics-missing", {
                    headers: { "X-Correlation-ID": "agent-diagnostics-proof" },
                });
            });
        await page.locator("body").waitFor({ state: "visible" });
        const content = await page.locator("body").innerText();
        if (
            content.trim().length < 20 ||
            /Error Overlay|Failed to compile/u.test(content)
        )
            throw new Error(
                "Rendered task application is blank or has a framework overlay",
            );
        return {
            renderedApiStatus: response.status(),
            ownedIdentityMatches: true,
            pageUrl: page.url(),
            pageTitle: await page.title(),
            diagnostics: [...events],
        };
    } finally {
        onDiagnostics(events);
        await browser.close();
    }
}
