import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { type APIResponse, expect, test as base } from '@playwright/test';

import { runtimeDiagnosticState } from '../../../scripts/ai/runtime-diagnostics.mjs';
import { HTTP_OK, wireRecord, wireString } from './wire-values';

const LOGIN_CACHE_MILLISECONDS = 60_000;
const browserHeaders = { 'X-Requested-With': 'XMLHttpRequest' };
let cachedLogin: { runtime: string; headers: Record<string, string>; expires: number } | null = null;

export const test = base.extend<{
    owned: { frontendUrl: string; headers: Record<string, string>; credentials: { email: string; password: string } };
}>({
    owned: async function ownedAsync({ request }, use) {
        const name = process.env['FOODDIARY_E2E_RUNTIME'];
        if (name === undefined || name.length === 0) {
            throw new Error('Run through scripts/ai/verify-user-journeys.mjs with an owned runtime');
        }
        const root = resolve('..');
        const state = runtimeDiagnosticState(name, root);
        if (state.status !== 'ready' || state.staleSources || state.processes.some(process => !process.alive)) {
            throw new Error('Live journeys require a ready, current, owned task runtime');
        }
        // eslint-disable-next-line security/detect-non-literal-fs-filename -- runtimeDiagnosticState validates the slug, checkout and owned endpoint first.
        const input: unknown = JSON.parse(readFileSync(resolve(root, `.artifacts/task-runtime/${name}/credentials.json`), 'utf8'));
        const credentialRecord = wireRecord(input);
        const credentials = { email: wireString(credentialRecord['email']), password: wireString(credentialRecord['password']) };
        const runtime = `${name}:${state.frontendUrl}`;
        if (cachedLogin?.runtime !== runtime || cachedLogin.expires <= Date.now()) {
            const login = await request.post(`${state.frontendUrl}/api/v1/auth/login`, { data: credentials, headers: browserHeaders });
            expect(login.status()).toBe(HTTP_OK);
            const start = await request.post(`${state.frontendUrl}/api/v1/auth/admin-sso/start`, { headers: browserHeaders });
            expect(start.status()).toBe(HTTP_OK);
            const code = wireString(wireRecord(await start.json())['code']);
            const admin = await request.post(`${state.frontendUrl}/api/v1/auth/admin-sso/exchange`, {
                data: { code },
                headers: browserHeaders,
            });
            expect(admin.status()).toBe(HTTP_OK);
            cachedLogin = {
                runtime,
                headers: { Cookie: `${readSessionCookie(login)  }; ${  readSessionCookie(admin, true)}`, ...browserHeaders },
                expires: Date.now() + LOGIN_CACHE_MILLISECONDS,
            };
        }
        await use({ frontendUrl: state.frontendUrl, headers: cachedLogin.headers, credentials });
    },
    page: async function pageAsync({ page, owned }, use) {
        const login = await page
            .context()
            .request.post(`${owned.frontendUrl  }/api/v1/auth/login`, { data: owned.credentials, headers: browserHeaders });
        expect(login.status()).toBe(HTTP_OK);
        await use(page);
    },
});

export function readSessionCookie(response: APIResponse, admin = false): string {
    const name = admin ? 'fooddiary.admin=' : 'fooddiary.browser=';
    const cookies = response
        .headersArray()
        .filter(
            header =>
                header.name.toLowerCase() === 'set-cookie' && (header.value.startsWith(name) || header.value.startsWith(`__Host-${  name}`)),
        );
    if (cookies.length !== 1) {throw new Error('Expected a single owned browser session cookie');}
    return cookies[0].value.split(';')[0];
}

export { expect } from '@playwright/test';
