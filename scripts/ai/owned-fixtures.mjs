import { randomBytes, randomUUID } from 'node:crypto';
import { closeSync, existsSync, mkdirSync, openSync, readFileSync, renameSync, unlinkSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';
import { repositoryRoot } from './generate-feature.mjs';
import { ownedApi, ownedRuntime } from './owned-runtime-client.mjs';
import { requestOwnedApi as request, ownedAdminToken as adminToken } from './owned-api-requests.mjs';
import { redactDiagnostic } from './diagnostic-redaction.mjs';
import { cleanupFixtureResidue, fixtureOwnershipProof, verifyFixtureOwnership } from './owned-fixture-residue.mjs';
import { fixtureIdentities, observeFixtureObjects, queryOwnedDatabase, scanFixtureReferences, summarizeFixtureCleanup, validateFixtureObjects, UUID } from './owned-fixture-evidence.mjs';

const save = (path, value) => { writeFileSync(`${path}.tmp`, JSON.stringify(value, null, 2), { mode: 0o600 }); renameSync(`${path}.tmp`, path); };
const permissions = { shareProfile: true, shareMeals: true, shareStatistics: true, shareWeight: true, shareWaist: true, shareGoals: true, shareHydration: true, shareFasting: true };
const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAIAAAD8GO2jAAAAKklEQVR4nGMIqFhAU8QwasGoBaMWjFowasGoBaMWjFowasGoBaMWDBULAMumoEz6MJc8AAAAAElFTkSuQmCC', 'base64');

export function fixturePlan(runId) {
    if (!UUID.test(runId)) throw new Error('Expected a lowercase fixture run UUID');
    return {
        marker: `webmcp-fixture-${runId}`,
        period: { dateFrom: '2025-01-01', dateTo: '2025-01-25', timeZone: 'UTC' },
        expected: { trackedDays: 25, mealCount: 25, totalCalories: 3850, averageCalories: 154, childRecipeCalories: 308, parentRecipeCalories: 77 },
        dates: Array.from({ length: 25 }, (_, index) => `2025-01-${String(index + 1).padStart(2, '0')}T12:00:00Z`),
    };
}

export async function runFixtures(action, name, runId) {
    if (!['create', 'verify', 'cleanup'].includes(action)) throw new Error('Choose create, verify or cleanup');
    const plan = fixturePlan(runId);
    const state = ownedRuntime(name);
    const directory = join(state.directory, 'fixtures', runId);
    mkdirSync(directory, { recursive: true });
    const path = join(directory, 'manifest.json');
    if (action !== 'create' && !existsSync(path)) throw new Error('The requested fixture manifest does not exist');
    const lockPath = join(state.directory, 'fixtures', 'lifecycle.lock');
    const lock = openSync(lockPath, 'wx', 0o600);
    try {
        const manifest = existsSync(path) ? JSON.parse(readFileSync(path, 'utf8')) : {
            schemaVersion: 1, checkout: repositoryRoot, runtimeId: state.id, runId, status: 'creating', accounts: {}, entities: [], objects: [], ...plan,
        };
        if (manifest.runtimeId !== state.id || manifest.runId !== runId || resolve(manifest.checkout) !== resolve(repositoryRoot)) throw new Error('Fixture manifest ownership mismatch');
        fixtureIdentities(manifest);
        validateFixtureObjects(manifest);
        save(path, manifest);
        const credentialsPath = join(directory, 'credentials.json');
        const credentials = existsSync(credentialsPath) ? JSON.parse(readFileSync(credentialsPath, 'utf8')) : {};
        const checkpoint = () => save(path, manifest);
        let readToken;
        const step = async (key, operation) => {
            if (manifest.pending?.step !== key) { manifest.pending = { step: key, key: randomUUID() }; checkpoint(); }
            const value = await operation(manifest.pending.key);
            return value;
        };
        if (action === 'create') {
            if (manifest.status === 'cleaned' || manifest.status === 'cleaning') throw new Error('A cleaned fixture cannot be recreated with the same run ID');
            const client = await ownedApi(name);
            const admin = await adminToken(client);
            for (const [label, roles] of [['dietologist', ['Dietologist']], ['client', []]]) {
                if (manifest.accounts[label]) continue;
                credentials[label] ??= { email: `${plan.marker}-${label}@example.test`, password: `Fixture!${randomBytes(24).toString('base64url')}` };
                save(credentialsPath, credentials);
                const existing = await request(state, admin, 'GET', `/api/v1/admin/users?limit=20&includeDeleted=true&search=${encodeURIComponent(credentials[label].email)}`);
                const matches = existing.data.filter(account => account.email === credentials[label].email);
                if (matches.length > 1) throw new Error('Fixture account identity is ambiguous');
                const result = matches.length === 1 ? { user: matches[0] } : await step(`account-${label}`, key => request(state, admin, 'POST', '/api/v1/admin/users', {
                    email: credentials[label].email, temporaryPassword: credentials[label].password, firstName: 'Fixture', lastName: label, language: 'en', roles,
                    generatePassword: false, isEmailConfirmed: true, sendCredentialsEmail: false, requirePasswordChange: false,
                }, key));
                if (!UUID.test(result.user?.id)) throw new Error('Fixture account identity is unavailable');
                manifest.accounts[label] = { id: result.user.id, roles }; delete manifest.pending; checkpoint();
            }
            const tokens = {};
            for (const label of ['client', 'dietologist']) {
                tokens[label] = (await request(state, null, 'POST', '/api/v1/auth/login', credentials[label])).accessToken;
                if ((await request(state, tokens[label], 'GET', '/api/v1/users/info')).id !== manifest.accounts[label].id) throw new Error('Fixture credentials do not match the owned account');
            }
            readToken = tokens.client;
            if (!manifest.imageAssetId || !manifest.imageUploaded) {
                const upload = await step('image-url', key => request(state, tokens.client, 'POST', '/api/v1/images/upload-url', { fileName: 'fixture.png', contentType: 'image/png', fileSizeBytes: png.length }, key));
                const url = new URL(upload.uploadUrl);
                if (url.origin !== `http://127.0.0.1:${state.ports.storage}`) throw new Error('Fixture upload refused a foreign storage origin');
                const key = decodeURIComponent(url.pathname.split('/').slice(2).join('/'));
                if (!key.startsWith(`users/${manifest.accounts.client.id}/images/`)) throw new Error('Fixture upload ownership mismatch');
                if (manifest.imageAssetId && manifest.imageAssetId !== upload.assetId) throw new Error('Resumed upload returned a different asset');
                if (!manifest.imageAssetId) {
                    manifest.imageAssetId = upload.assetId;
                    manifest.entities.push({ kind: 'image', id: upload.assetId });
                    manifest.objects.push({ bucket: 'fooddiary-staging', key }, { bucket: 'fooddiary-public', key });
                }
                checkpoint();
                const put = await fetch(url, { method: 'PUT', body: png, headers: { 'content-type': 'image/png' }, redirect: 'error', signal: AbortSignal.timeout(30_000) });
                if (!put.ok) throw new Error('Fixture image upload failed');
                manifest.imageUploaded = true; delete manifest.pending; checkpoint();
            }
            if (!manifest.imageConfirmed) {
                if (!manifest.imageUploaded) throw new Error('Interrupted upload needs a new fixture run; cleanup can reclaim the pending asset');
                await step('image-confirm', key => request(state, tokens.client, 'POST', `/api/v1/images/${manifest.imageAssetId}/confirm`, {}, key));
                manifest.imageConfirmed = true; delete manifest.pending; checkpoint();
            }
            const createEntity = async (kind, route, body) => {
                if (manifest[kind]) return manifest[kind];
                const result = await step(kind, key => request(state, tokens.client, 'POST', route, body, key));
                if (!UUID.test(result.id)) throw new Error('Fixture entity identity is unavailable');
                manifest[kind] = result.id; manifest.entities.push({ kind, id: result.id }); delete manifest.pending; checkpoint();
                return result.id;
            };
            const productId = await createEntity('product', '/api/v1/products', { name: `${plan.marker} oats`, productType: 'Grain', visibility: 'Private', baseUnit: 'G', baseAmount: 100, defaultPortionAmount: 100, caloriesPerBase: 154, proteinsPerBase: 10, fatsPerBase: 6, carbsPerBase: 15, fiberPerBase: 2, imageAssetId: manifest.imageAssetId });
            const recipe = (name, servings, ingredients) => ({ name: `${plan.marker} ${name}`, servings, cookTime: 1, language: 'en', visibility: 'Private', calculateNutritionAutomatically: true, steps: [{ description: 'Synthetic fixture', ingredients }] });
            const childId = await createEntity('childRecipe', '/api/v1/recipes', recipe('child', 2, [{ productId, amount: 200 }]));
            await createEntity('parentRecipe', '/api/v1/recipes', recipe('parent', 1, [{ nestedRecipeId: childId, amount: 0.5 }]));
            for (const [index, date] of plan.dates.entries()) await createEntity(`meal-${index + 1}`, '/api/v1/meals', { date, mealType: 'LUNCH', isNutritionAutoCalculated: true, items: [{ productId, amount: 100 }] });
            if (!manifest.relationshipAccepted) {
                let relationship = await request(state, tokens.client, 'GET', '/api/v1/dietologist/relationship');
                if (relationship === null) {
                    await step('invitation', key => request(state, tokens.client, 'POST', '/api/v1/dietologist/invite', { dietologistEmail: credentials.dietologist.email, permissions }, key));
                    relationship = await request(state, tokens.client, 'GET', '/api/v1/dietologist/relationship');
                }
                if (!UUID.test(relationship?.invitationId)) throw new Error('Fixture relationship identity is unavailable');
                if (!manifest.relationship) { manifest.relationship = relationship.invitationId; manifest.entities.push({ kind: 'relationship', id: relationship.invitationId }); checkpoint(); }
                if (relationship.status !== 'Accepted') await request(state, tokens.dietologist, 'POST', `/api/v1/dietologist/invitations/${relationship.invitationId}/accept-current-user`, {}, randomUUID());
                manifest.relationshipAccepted = true; delete manifest.pending; checkpoint();
            }
            manifest.status = 'ready'; checkpoint();
        }
        if (action === 'cleanup') {
            manifest.status = 'cleaning'; checkpoint();
            const userIds = Object.values(manifest.accounts).map(account => `'${account.id}'`).join(',');
            const lifecycle = queryOwnedDatabase(state, `SELECT coalesce(json_agg(metadata), '[]') FROM (SELECT "Id" AS id, "Email" IN ('${plan.marker}-client@example.test','${plan.marker}-dietologist@example.test') AS owned, ("DeletedAt" IS NOT NULL AND NOT "IsActive") AS deleted FROM "Users" WHERE "Id" IN (${userIds || 'NULL'}) OR "Email" IN ('${plan.marker}-client@example.test','${plan.marker}-dietologist@example.test')) metadata`);
            if (lifecycle.some(account => account.owned !== true)) throw new Error('Cleanup refuses a foreign manifest account');
            if (lifecycle.some(account => !Object.values(manifest.accounts).some(recorded => recorded.id === account.id))) throw new Error('Resume creation to checkpoint an account whose creation result was interrupted');
            const ownershipSecret = JSON.parse(readFileSync(join(state.directory, 'credentials.json'), 'utf8')).password;
            if (!manifest.ownershipProof) {
                if (lifecycle.length !== Object.keys(manifest.accounts).length) throw new Error('Cannot establish ownership after an unsealed account has disappeared');
                manifest.ownershipProof = fixtureOwnershipProof(manifest, ownershipSecret); checkpoint();
            }
            verifyFixtureOwnership(manifest, ownershipSecret);
            for (const label of Object.keys(manifest.accounts)) {
                const account = lifecycle.find(item => item.id === manifest.accounts[label].id);
                if (account === undefined || account.deleted) { manifest.accounts[label].softDeleted = true; checkpoint(); continue; }
                const auth = await request(state, null, 'POST', '/api/v1/auth/login', credentials[label]);
                if ((await request(state, auth.accessToken, 'GET', '/api/v1/users/info')).id !== manifest.accounts[label].id) throw new Error('Cleanup credentials do not match the manifest account');
                await request(state, auth.accessToken, 'DELETE', '/api/v1/users');
                manifest.accounts[label].softDeleted = true; checkpoint();
            }
            const scope = state.apiDiagnosticsBuild?.artifactScope ?? join(state.directory, 'build', 'bin', 'FoodDiary.Web.Api', 'debug');
            if (!resolve(scope).startsWith(`${resolve(state.directory)}\\`) && !resolve(scope).startsWith(`${resolve(state.directory)}/`)) throw new Error('The API binary must belong to the owned runtime');
            const assembly = join(scope, 'FoodDiary.Web.Api.dll');
            const result = userIds.length === 0 ? { status: 0 } : spawnSync('dotnet', [assembly, '--purge-owned-fixtures', path], { cwd: repositoryRoot, env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development', FOODDIARY_TASK_CONFIG: join(state.directory, 'appsettings.json') }, encoding: 'utf8', windowsHide: true, timeout: 120_000, maxBuffer: 512 * 1024 });
            if (result.status !== 0) {
                writeFileSync(join(directory, 'purge-diagnostics.log'), redactDiagnostic(`${result.stdout ?? ''}\n${result.stderr ?? ''}`, 12_000), { mode: 0o600 });
                throw new Error('Scoped owner purge failed; inspect the redacted purge-diagnostics.log beside the manifest and resume cleanup');
            }
            manifest.residueCleanup = cleanupFixtureResidue(state, manifest, ownershipSecret); checkpoint();
        }
        const references = scanFixtureReferences(state, manifest);
        const objects = observeFixtureObjects(state, manifest);
        const observation = summarizeFixtureCleanup(references, objects);
        const keys = manifest.objects.map(item => `'${item.key.replaceAll("'", "''")}'`);
        const pendingImageDeletions = keys.length === 0 ? 0 : queryOwnedDatabase(state, `SELECT count(*)::int FROM "ImageObjectDeletionOutbox" WHERE "ObjectKey" IN (${keys.join(',')}) AND "ProcessedOnUtc" IS NULL`);
        const cleanupComplete = observation.databaseClear && observation.storageClear && pendingImageDeletions === 0;
        let statistics = null;
        let recipeNutrition = null;
        if (action !== 'cleanup' && manifest.status !== 'cleaned' && manifest.accounts.client) {
            readToken ??= (await request(state, null, 'POST', '/api/v1/auth/login', credentials.client)).accessToken;
            const summary = await request(state, readToken, 'GET', '/api/v1/statistics/summary?dateFrom=2025-01-01T00%3A00%3A00Z&dateTo=2025-01-25T23%3A59%3A59.999Z&quantizationDays=1&timeZoneId=UTC');
            const trackedDays = summary.nutrition.reduce((sum, bucket) => sum + (bucket.trackedDayCount ?? 0), 0);
            const totalCalories = summary.nutrition.reduce((sum, bucket) => sum + bucket.totalCalories, 0);
            statistics = { trackedDays, totalCalories, averageCalories: trackedDays === 0 ? null : totalCalories / trackedDays };
            if (manifest.childRecipe && manifest.parentRecipe) {
                const [child, parent] = await Promise.all([manifest.childRecipe, manifest.parentRecipe].map(id => request(state, readToken, 'GET', `/api/v1/recipes/${id}`)));
                recipeNutrition = { childRecipeCalories: child.totalCalories, parentRecipeCalories: parent.totalCalories };
            }
        }
        const statisticsMatches = statistics !== null && ['trackedDays', 'totalCalories', 'averageCalories'].every(key => statistics[key] === plan.expected[key]);
        const recipeNutritionMatches = recipeNutrition !== null && ['childRecipeCalories', 'parentRecipeCalories'].every(key => recipeNutrition[key] === plan.expected[key]);
        if (action === 'cleanup' && cleanupComplete) { manifest.status = 'cleaned'; checkpoint(); }
        const report = { runId, runtimeId: state.id, fixtureStatus: manifest.status, expected: plan.expected, statistics, statisticsMatches, recipeNutrition, recipeNutritionMatches, observed: observation, pendingImageDeletions, cleanupComplete };
        save(join(directory, 'verification.json'), report);
        return { ...report, manifestPath: path };
    } finally { closeSync(lock); unlinkSync(lockPath); }
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
    try {
        const [action, flag, name, runFlag, suppliedRun] = process.argv.slice(2);
        if (flag !== '--name' || (runFlag !== '--run' && runFlag !== undefined) || (action !== 'create' && !suppliedRun)) throw new Error('Use owned-fixtures.mjs create|verify|cleanup --name <runtime> [--run <UUID>]');
        const result = await runFixtures(action, name, suppliedRun ?? randomUUID());
        console.log(JSON.stringify(result, null, 2));
        if ((action === 'create' && (!result.statisticsMatches || !result.recipeNutritionMatches)) || (action === 'cleanup' && !result.cleanupComplete)) process.exitCode = 1;
    } catch (error) { console.error(error instanceof Error && !error.message.includes('http') ? error.message : 'Owned fixture operation failed; no private transport output is exposed'); process.exitCode = 1; }
}
