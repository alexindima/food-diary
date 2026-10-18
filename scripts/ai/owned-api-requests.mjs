import { readFileSync } from 'node:fs';
import { OpenApiFixtures } from '../../FoodDiary.Web.Client/e2e/support/openapi-fixtures.mjs';

const userContract = new OpenApiFixtures();
const adminContract = new OpenApiFixtures(JSON.parse(readFileSync(new URL('../../FoodDiary.Web.Client/api-sdk/admin.openapi.json', import.meta.url), 'utf8')));

export async function requestOwnedApi(state, token, method, path, body, key) {
    if (!Number.isSafeInteger(state.ports?.api) || state.apiUrl !== `http://127.0.0.1:${state.ports.api}`) throw new Error('Owned API origin must match the local runtime port');
    if (!path.startsWith('/api/v1/') || path.includes('://')) throw new Error('Only relative owned API routes are allowed');
    const contract = path.startsWith('/api/v1/admin/') || path === '/api/v1/admin/users' ? adminContract : userContract;
    const operation = contract.operation(method, path);
    const schema = operation.requestBody?.content?.['application/json']?.schema;
    if (schema !== undefined) contract.validate(schema, body, operation.operationId);
    const send = () => fetch(`${state.apiUrl}${path}`, {
        method, headers: { 'content-type': 'application/json', ...(token ? { authorization: `Bearer ${token}` } : {}), ...(key ? { 'Idempotency-Key': key } : {}) },
        ...(body === undefined ? {} : { body: JSON.stringify(body) }), redirect: 'error', signal: AbortSignal.timeout(30_000),
    });
    let response = await send();
    for (let attempt = 0; response.status === 429 && attempt < 2; attempt++) {
        const seconds = Math.min(60, Math.max(1, Number(response.headers.get('retry-after')) || 60));
        await new Promise(resolveWait => setTimeout(resolveWait, seconds * 1000));
        response = await send();
    }
    if (!response.ok) throw new Error(`Owned API ${method} failed (HTTP ${response.status})`);
    const result = response.status === 204 ? undefined : await response.json();
    contract.assertResponse(method, path, response.status, result);
    return result ?? null;
}

export async function ownedAdminToken(client) {
    const sso = await client.request('POST', '/api/v1/auth/admin-sso/start', {});
    return (await requestOwnedApi(client.state, null, 'POST', '/api/v1/auth/admin-sso/exchange', { code: sso.code })).accessToken;
}
