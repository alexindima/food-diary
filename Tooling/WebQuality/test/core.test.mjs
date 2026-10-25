import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { gitBlob, within, writeJson } from '../lib/files.mjs';
import { REPOSITORY, validateSnapshot, compareSnapshots, buildSnapshot, adoptSnapshot, assertSeparateStage } from '../lib/upstream.mjs';
import { validatePolicy, initialResults, mergeResults, exitCode } from '../lib/policy.mjs';
import { coverage, renderReport } from '../lib/report.mjs';
import { parseArgs } from '../cli.mjs';

const text = '# Accessible images\nProvide alternative text.\n';
const blob = gitBlob(text);
const lock = {
    schemaVersion: 1,
    repository: REPOSITORY,
    commit: 'a'.repeat(40),
    version: '1.0',
    guides: [
        { id: 'images', category: 'html', path: 'skills/modern-web-guidance/guides/html/images.md', blob, cache: `sources/${blob}.md` },
        { id: 'other', category: 'html', path: 'skills/modern-web-guidance/guides/html/other.md', blob: 'b'.repeat(40) },
    ],
};
const policy = {
    schemaVersion: 1,
    scopes: [{ id: 'client', path: 'client/src' }],
    requirements: [
        {
            id: 'image-alt',
            source: { guide: 'images', anchor: 'alternative text', reviewedBlob: blob },
            engine: 'source',
            check: 'template:alt-text',
            statement: 'Provide alternative text.',
            severity: 'error',
            scopes: ['client'],
        },
    ],
    exceptions: [],
};
const clone = value => structuredClone(value);

async function directory(t) {
    const root = await mkdtemp(path.join(tmpdir(), 'fd-web-quality-'));
    t.after(async () => {
        assert.equal(path.dirname(root), tmpdir());
        await rm(root, { recursive: true, force: true });
    });
    return root;
}
async function snapshot(t, directoryName = '') {
    const root = path.join(await directory(t), directoryName);
    await mkdir(path.join(root, 'sources'), { recursive: true });
    await writeFile(path.join(root, `sources/${blob}.md`), text);
    await writeJson(path.join(root, 'upstream.lock.json'), lock);
    return root;
}

test('argument parsing rejects typos, duplicate flags, unknown profiles at execution and missing values', () => {
    assert.deepEqual(parseArgs(['audit', '--profile=source', '--strict']), {
        command: 'audit',
        options: { profile: 'source', strict: true },
    });
    for (const args of [
        ['unknown'],
        ['audit', '--strcit'],
        ['audit', '--strict=false'],
        ['audit', '--profile'],
        ['audit', '--profile=x', '--profile=y'],
    ])
        assert.throws(() => parseArgs(args));
});

test('full catalogue decisions cannot omit guides, source anchors, owner evidence or executable absence guards', async t => {
    const root = await snapshot(t, 'Tooling/WebQuality/upstream');
    await mkdir(path.resolve(root, '../../../client/src'), { recursive: true });
    const fullLock = { ...clone(lock), guides: [clone(lock.guides[0])] };
    const full = { ...clone(policy), reviewMode: 'all-guides', guideReviews: [{
        guide: 'images', reviewedBlob: blob, status: 'reviewed', reason: 'Images are rendered by client templates.',
        anchor: 'alternative text', requirements: ['image-alt'], evidence: ['client/src'],
    }] };
    await validatePolicy(full, fullLock, root);
    for (const field of ['anchor', 'requirements', 'evidence']) {
        const bad = clone(full); delete bad.guideReviews[0][field];
        await assert.rejects(validatePolicy(bad, fullLock, root), /Incomplete/u);
    }
    const missing = clone(full); missing.guideReviews = [];
    await assert.rejects(validatePolicy(missing, fullLock, root), /Every pinned guide/u);
    const stale = clone(full); stale.guideReviews[0].reviewedBlob = 'f'.repeat(40);
    await assert.rejects(validatePolicy(stale, fullLock, root), /stale/u);
    const proof = clone(full); proof.guideReviews[0].evidence = ['client/missing'];
    await assert.rejects(validatePolicy(proof, fullLock, root));
    const unmapped = clone(full); unmapped.guideReviews[0].requirements = ['invented-pass'];
    await assert.rejects(validatePolicy(unmapped, fullLock, root), /missing assertions/u);

    fullLock.guides.push({ ...clone(fullLock.guides[0]), id: 'optional' });
    full.requirements.push({ ...clone(full.requirements[0]), id: 'adoption', check: 'source:guide-applicability' });
    full.guideReviews.push({ ...clone(full.guideReviews[0]), guide: 'optional', status: 'not-applicable', requirements: ['adoption'],
        whenAbsent: { reason: 'Native optional API is not used.', patterns: ['\\bNewFeature\\b'], examples: ['NewFeature.create()'] } });
    await validatePolicy(full, fullLock, root);
    const noProbe = clone(full); delete noProbe.guideReviews[1].whenAbsent;
    await assert.rejects(validatePolicy(noProbe, fullLock, root), /executable/u);
    const noFixture = clone(full); noFixture.guideReviews[1].whenAbsent.examples = [];
    await assert.rejects(validatePolicy(noFixture, fullLock, root), /positive fixture/u);
    const noGuard = clone(full); noGuard.guideReviews[1].requirements = ['image-alt'];
    await assert.rejects(validatePolicy(noGuard, fullLock, root), /executable guard/u);
});

test('portable paths cannot escape or point to the root', () => {
    for (const value of ['../outside', '..', '.', 'a/../../outside', '/outside', 'a\\b', '']) assert.throws(() => within('/root', value));
    assert.equal(within('/root', 'inside/data.json'), path.resolve('/root/inside/data.json'));
});

test('upstream staging cannot write inside or around accepted source directories', () => {
    assertSeparateStage('/staged/candidate', '/accepted/guides');
    for (const candidate of ['/accepted', '/accepted/guides', '/accepted/guides/candidate'])
        assert.throws(() => assertSeparateStage(candidate, '/accepted/guides'));
});

test('offline snapshot validates actual Git blobs and rejects changed cache bytes', async t => {
    const root = await snapshot(t);
    await validateSnapshot(root, lock);
    await validatePolicy(policy, lock, root);
    await writeFile(path.join(root, `sources/${blob}.md`), `${text}changed`);
    await assert.rejects(validateSnapshot(root, lock), /content changed/u);
});

test('stale mappings, missing anchors, duplicates, unknown scopes and expired exceptions fail validation', async t => {
    const root = await snapshot(t);
    for (const mutate of [
        value => {
            value.requirements[0].source.reviewedBlob = 'c'.repeat(40);
        },
        value => {
            value.requirements[0].source.anchor = 'not in source';
        },
        value => {
            value.requirements.push(clone(value.requirements[0]));
        },
        value => {
            value.requirements[0].scopes = ['absent'];
        },
        value => {
            value.requirements[0].engine = 'unknown';
        },
        value => {
            value.exceptions = [{ requirement: 'image-alt', scope: 'x', reason: 'debt', owner: 'team', expires: '2000-01-01' }];
        },
    ]) {
        const value = clone(policy);
        mutate(value);
        await assert.rejects(validatePolicy(value, lock, root));
    }
});

test('guide diff reports added changed and removed guides with dependent requirements', () => {
    const next = clone(lock);
    next.guides[0].blob = 'c'.repeat(40);
    next.guides[1] = { id: 'new', blob: 'd'.repeat(40), path: 'new.md' };
    const diff = compareSnapshots(lock, next, policy.requirements);
    assert.deepEqual(
        diff.map(item => [item.id, item.change, item.requirements]),
        [
            ['images', 'changed', ['image-alt']],
            ['new', 'added', []],
            ['other', 'removed', []],
        ],
    );
    assert.deepEqual(compareSnapshots(lock, clone(lock), policy.requirements), []);
});

test('candidate snapshot rejects truncated trees and mismatched downloaded blobs', async () => {
    const packageText = '{"version":"1.0"}';
    const tree = {
        sha: 'a'.repeat(40),
        truncated: false,
        tree: [
            { type: 'blob', path: lock.guides[0].path, sha: blob },
            { type: 'blob', path: 'package.json', sha: gitBlob(packageText) },
        ],
    };
    const fetcher = async url =>
        new Response(
            url.includes('/commits/')
                ? JSON.stringify({ sha: 'e'.repeat(40), commit: { tree: { sha: 'a'.repeat(40) } } })
                : url.includes('/git/trees/')
                  ? JSON.stringify(tree)
                  : url.endsWith('/package.json')
                    ? packageText
                    : text,
        );
    const built = await buildSnapshot('v1', ['images'], fetcher);
    assert.equal(built.lock.guides.length, 1);
    assert.equal(built.lock.commit, 'e'.repeat(40));
    assert.equal(built.lock.tree, 'a'.repeat(40));
    assert.equal(built.texts.get(`sources/${blob}.md`), text);
    tree.truncated = true;
    await assert.rejects(buildSnapshot('v1', ['images'], fetcher), /truncated/u);
    tree.truncated = false;
    tree.tree[0].sha = 'c'.repeat(40);
    await assert.rejects(buildSnapshot('v1', ['images'], fetcher), /mismatch/u);
});

test('adoption preserves accepted snapshot when dependent policy review is stale', async t => {
    const accepted = await snapshot(t, 'accepted');
    const candidate = await snapshot(t, 'candidate');
    const previous = await readFile(path.join(accepted, 'upstream.lock.json'), 'utf8');
    const stale = clone(policy);
    stale.requirements[0].source.reviewedBlob = 'c'.repeat(40);
    await assert.rejects(adoptSnapshot(candidate, accepted, stale, validatePolicy, 'reviewed'), /stale/u);
    assert.equal(await readFile(path.join(accepted, 'upstream.lock.json'), 'utf8'), previous);
    await assert.rejects(adoptSnapshot(candidate, accepted, policy, validatePolicy, ''), /review note/u);
    await adoptSnapshot(candidate, accepted, policy, validatePolicy, 'Checked same-version adoption.');
    assert.equal(JSON.parse(await readFile(path.join(accepted, 'review.json'), 'utf8')).commit, lock.commit);
});

test('missing evidence stays not-run; empty, forged or duplicate passing evidence is rejected', () => {
    const initial = initialResults(policy);
    assert.equal(initial[0].status, 'not-run');
    const pass = { requirement: 'image-alt', engine: 'source', status: 'pass', observations: 1, findings: [] };
    assert.equal(mergeResults(policy, initial, [pass])[0].status, 'pass');
    for (const records of [
        [{ ...pass, observations: 0 }],
        [pass, pass],
        [{ ...pass, requirement: 'unknown' }],
        [{ ...pass, engine: 'browser' }],
    ])
        assert.throws(() => mergeResults(policy, initial, records));
    assert.equal(exitCode(initial, { complete: true }), 1);
    assert.equal(exitCode([{ ...pass, status: 'error' }]), 2);
    assert.equal(exitCode([{ ...pass, status: 'fail', severity: 'warning' }]), 0);
    assert.equal(exitCode([{ ...pass, status: 'fail', severity: 'warning' }], { strict: true }), 1);
});

test('exceptions apply only to exact declared findings and never turn engine errors into passes', () => {
    const value = clone(policy);
    value.exceptions = [{ requirement: 'image-alt', scope: 'client/a.html', owner: 'team', reason: 'reviewed', expires: '2099-01-01' }];
    const failed = {
        requirement: 'image-alt',
        engine: 'source',
        status: 'fail',
        observations: 1,
        findings: [{ file: 'client/a.html', message: 'missing alt' }],
    };
    assert.equal(mergeResults(value, initialResults(value), [failed])[0].status, 'exception');
    failed.findings.push({ file: 'client/b.html', message: 'missing alt' });
    assert.equal(mergeResults(value, initialResults(value), [failed])[0].status, 'fail');
});

test('HTML escapes evidence and reports partial and unreviewed coverage without a conformance score', () => {
    const report = {
        upstream: lock,
        catalogue: coverage(lock, policy),
        results: initialResults(policy),
        provenance: { revision: 'a'.repeat(40) },
        executions: {},
        startedAt: '2026-10-25',
    };
    report.results[0].detail = '<script>alert("evidence")</script>';
    const html = renderReport(report, policy);
    assert.ok(html.includes('&lt;script&gt;'));
    assert.ok(!html.includes('<script>alert'));
    assert.ok(html.includes('1 partially mapped'));
    assert.ok(html.includes('1 unreviewed'));
    assert.ok(html.includes('not-run'));
});
