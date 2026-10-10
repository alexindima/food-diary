import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { expect, test as base } from '@playwright/test';

import { runtimeDiagnosticState } from '../../../scripts/ai/runtime-diagnostics.mjs';
import { HTTP_OK, wireRecord, wireString } from './wire-values';

const LOGIN_CACHE_MILLISECONDS = 60_000;
let cachedLogin: { runtime: string; token: string; expires: number } | null = null;

export const test = base.extend<{ owned: { frontendUrl: string; token: string; credentials: { email: string; password: string } } }>({
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
            const login = await request.post(`${state.frontendUrl}/api/v1/auth/login`, { data: credentials });
            expect(login.status()).toBe(HTTP_OK);
            const auth = wireRecord(await login.json());
            cachedLogin = { runtime, token: wireString(auth['accessToken']), expires: Date.now() + LOGIN_CACHE_MILLISECONDS };
        }
        await use({ frontendUrl: state.frontendUrl, token: cachedLogin.token, credentials });
    },
    page: async function pageAsync({ page, owned }, use) {
        await page.addInitScript(token => {
            localStorage.setItem('authToken', token);
        }, owned.token);
        await use(page);
    },
});

export { expect } from '@playwright/test';
