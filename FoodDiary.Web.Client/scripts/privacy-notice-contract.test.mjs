import test from 'node:test';
import { consentingPrivacyFixture } from '../src/testing/api-fixtures.ts';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const read = async path => readFile(new URL(path, import.meta.url), 'utf8');
const json = async path => JSON.parse(await read(path));
const select = (value, keys) => Object.fromEntries(keys.map(key => [key, value[key]]));

test('localized consent texts are archived under the exact client and server notice version', async () => {
    const client = await read('../src/app/shared/privacy/privacy-notice-version.ts');
    const server = await read('../../Modules/Users/Domain.Contracts/Privacy/PrivacyNoticeVersions.cs');
    const version = /PRIVACY_NOTICE_VERSION = '([^']+)'/u.exec(client)?.[1];
    assert.ok(version);
    assert.equal(consentingPrivacyFixture().noticeVersion, version);
    assert.equal(/public const string Current = "([^"]+)"/u.exec(server)?.[1], version);
    for (const locale of ['en', 'ru']) {
        const core = await json(`../assets/i18n/${locale}/core.json`);
        const app = await json(`../assets/i18n/${locale}/app.json`);
        const archived = await json(`../../docs/privacy/consent-notices/${version}.${locale}.json`);
        assert.equal(archived.version, version);
        assert.deepEqual(archived.health, select(core.PRIVACY_CHOICES, ['HEALTH_TITLE', 'HEALTH_TEXT', 'HEALTH_AGREEMENT', 'WITHDRAWAL']));
        assert.deepEqual(archived.marketing, select(core.MARKETING_CONSENT, ['TITLE', 'TEXT', 'ALLOW', 'REJECT']));
        assert.deepEqual(archived.ai, app.AI_CONSENT);
    }
});
