#!/usr/bin/env node
import { execFileSync } from 'node:child_process';
import { readFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { platform, arch } from 'node:os';
import { readJson, sha256, writeJson, listFiles, portable } from './lib/files.mjs';
import { buildSnapshot, saveSnapshot, compareSnapshots, validateSnapshot, adoptSnapshot, assertSeparateStage } from './lib/upstream.mjs';
import { validatePolicy, initialResults, mergeResults, exitCode } from './lib/policy.mjs';
import { coverage, renderReport } from './lib/report.mjs';
import { buildProduction, verifyBuild } from './lib/build.mjs';

const toolRoot = import.meta.dirname;
const repositoryRoot = path.resolve(toolRoot, '../..');
const snapshotDirectory = path.join(toolRoot, 'upstream');
const policyFile = path.join(toolRoot, 'policy.json');
const frontend = path.join(repositoryRoot, 'FoodDiary.Web.Client');
const adapter = file => import(pathToFileURL(path.join(frontend, 'quality/modern-web-guidance', file)).href);
const defaults = [
    'accessibility',
    'forms',
    'autofill-sign-in-form',
    'optimize-image-priority',
    'performance',
    'typography',
    'responsive-design',
    'respect-os-text-scale',
];

export function parseArgs(argv) {
    const command = argv[0] ?? 'help';
    const allowed = {
        help: [],
        validate: [],
        build: ['report-dir'],
        catalogue: ['search', 'guide'],
        snapshot: ['ref', 'output', 'cache'],
        update: ['ref', 'output', 'cache'],
        diff: ['candidate'],
        adopt: ['candidate', 'note'],
        audit: [
            'profile',
            'base-url',
            'static-root',
            'api-origin',
            'report-dir',
            'browser',
            'channel',
            'routes',
            'devices',
            'strict',
            'complete',
        ],
    };
    if (!Object.hasOwn(allowed, command)) throw new Error(`Unknown command: ${command}`);
    const options = {};
    for (let index = 1; index < argv.length; index++) {
        const match = /^--([a-z-]+)(?:=(.*))?$/u.exec(argv[index]);
        if (!match || !allowed[command].includes(match[1]) || Object.hasOwn(options, match[1]))
            throw new Error(`Unknown or repeated option: ${argv[index]}`);
        const [, key, inline] = match;
        if (['strict', 'complete'].includes(key)) {
            if (inline !== undefined) throw new Error(`${key} does not accept a value.`);
            options[key] = true;
        } else {
            const value = inline ?? argv[++index];
            if (!value || value.startsWith('--')) throw new Error(`Missing value for --${key}`);
            options[key] = value;
        }
    }
    return { command, options };
}

async function validate(lock, policy, directory = snapshotDirectory) {
    await validateSnapshot(directory, lock);
    await validatePolicy(policy, lock, directory);
    const source = await adapter('source.mjs');
    const browser = await adapter('browser.mjs');
    const contracts = await adapter('contracts.mjs');
    for (const requirement of policy.requirements) {
        const known =
            requirement.engine === 'source'
                ? source.sourceChecks
                : requirement.engine === 'browser'
                  ? browser.browserChecks
                  : requirement.engine === 'journeys'
                    ? new Set(['journeys:authenticated-axe'])
                    : requirement.engine === 'contracts'
                      ? contracts.contractChecks
                    : requirement.engine === 'performance'
                      ? new Set(['performance:public-budgets'])
                      : new Set([requirement.check]);
        if (!known.has(requirement.check)) throw new Error(`No implementation for ${requirement.id}: ${requirement.check}`);
    }
}

async function audit(options, lock, policy) {
    await validate(lock, policy);
    const selected =
        options.profile === 'all' ? ['source', 'contracts', 'browser', 'journeys', 'performance'] : (options.profile ?? 'source').split(',');
    if (
        !selected.length ||
        new Set(selected).size !== selected.length ||
        selected.some(engine => !['source', 'contracts', 'browser', 'journeys', 'performance'].includes(engine))
    )
        throw new Error('Unknown or repeated audit profile.');
    const startedAt = new Date().toISOString();
    const artifactDirectory = path.resolve(
        repositoryRoot,
        options['report-dir'] ?? `.artifacts/web-quality/runs/${startedAt.replace(/[:.]/gu, '-')}-${process.pid}`,
    );
    await mkdir(artifactDirectory, { recursive: true });
    const git = args => execFileSync('git', args, { cwd: repositoryRoot, encoding: 'utf8', windowsHide: true }).trim();
    const provenance = {
        revision: git(['rev-parse', 'HEAD']),
        dirty: Boolean(git(['status', '--porcelain'])),
        node: process.version,
        platform: `${platform()}/${arch()}`,
        policyFingerprint: sha256(await readFile(policyFile)),
        snapshotFingerprint: sha256(await readFile(path.join(snapshotDirectory, 'upstream.lock.json'))),
        frontendLockFingerprint: sha256(await readFile(path.join(frontend, 'package-lock.json'))),
    };
    const implementation = [
        ...(await listFiles(toolRoot, new Set(['.mjs']))),
        ...(await listFiles(path.join(frontend, 'quality/modern-web-guidance'), new Set(['.mjs']))),
    ];
    provenance.toolFingerprint = sha256(
        (
            await Promise.all(
                implementation.map(async file => `${portable(path.relative(repositoryRoot, file))}:${sha256(await readFile(file))}`),
            )
        )
            .sort()
            .join('\n'),
    );
    const results = initialResults(policy);
    const executed = [];
    const executions = {};
    for (const engine of selected) {
        console.log(`WebQuality: running ${engine}...`);
        let server;
        try {
            let run;
            const common = { repositoryRoot, policy, artifactDirectory };
            const staticRoot =
                !options['base-url'] && (engine === 'performance' || (engine === 'browser' && options['static-root']))
                    ? path.resolve(repositoryRoot, options['static-root'] ?? 'FoodDiary.Web.Client/dist/browser')
                    : undefined;
            const build = staticRoot ? await verifyBuild(repositoryRoot, staticRoot) : undefined;
            if (engine === 'source') run = await (await adapter('source.mjs')).runSource(common);
            if (engine === 'contracts') run = await (await adapter('contracts.mjs')).runContracts(common);
            if (engine === 'browser') {
                let baseURL = options['base-url'];
                if (!baseURL) {
                    if (!options['static-root'])
                        throw new Error('Browser audit needs --base-url or --static-root pointing at a production build.');
                    const module = await import(pathToFileURL(path.join(frontend, 'scripts/lighthouse-public-server.mjs')).href);
                    server = await module.startStaticServer(path.resolve(repositoryRoot, options['static-root']), options['api-origin']);
                    baseURL = server.origin;
                }
                const scenarios = await readJson(path.join(frontend, 'quality/modern-web-guidance/scenarios.json'));
                run = await (
                    await adapter('browser.mjs')
                ).runBrowser({ ...common, baseURL, scenarios, browserName: options.browser, channel: options.channel });
            }
            if (engine === 'journeys') run = await (await adapter('evidence.mjs')).runJourneys(common);
            if (engine === 'performance')
                run = await (
                    await adapter('evidence.mjs')
                ).runPerformance({ ...common, baseURL: options['base-url'], routes: options.routes, devices: options.devices, staticRoot });
            if (staticRoot) await verifyBuild(repositoryRoot, staticRoot);
            const expected = policy.requirements.filter(requirement => requirement.engine === engine).map(requirement => requirement.id);
            if (expected.some(id => !run.results.some(result => result.requirement === id)))
                throw new Error(`${engine} omitted required execution results.`);
            executed.push(...run.results);
            executions[engine] = run.metadata;
            if (['browser', 'performance'].includes(engine))
                executions[engine].sourceAssociation = build ?? 'External origin: checkout/source association is unverified.';
        } catch (error) {
            executions[engine] = { error: error.message };
            executed.push(
                ...policy.requirements
                    .filter(requirement => requirement.engine === engine)
                    .map(requirement => ({
                        requirement: requirement.id,
                        engine,
                        status: 'error',
                        observations: 0,
                        findings: [],
                        detail: error.message,
                    })),
            );
        } finally {
            await server?.close();
        }
    }
    const merged = mergeResults(policy, results, executed);
    const report = {
        schemaVersion: 1,
        startedAt,
        completedAt: new Date().toISOString(),
        profiles: selected,
        provenance,
        upstream: { repository: lock.repository, version: lock.version, commit: lock.commit },
        catalogue: coverage(lock, policy),
        executions,
        results: merged,
    };
    await writeJson(path.join(artifactDirectory, 'report.json'), report);
    await writeFileReport(path.join(artifactDirectory, 'index.html'), renderReport(report, policy));
    for (const result of merged)
        console.log(
            `${result.status.padEnd(10)} ${result.requirement}${result.findings.length ? ` (${result.findings.length} findings)` : ''}`,
        );
    console.log(`Report: ${path.join(artifactDirectory, 'index.html')}`);
    return (
        exitCode(merged, options) ||
        (options.complete && report.catalogue.some(guide => ['unreviewed', 'partial'].includes(guide.status)) ? 1 : 0)
    );
}

async function writeFileReport(file, text) {
    const { writeFile } = await import('node:fs/promises');
    await writeFile(file, text, 'utf8');
}

export async function main(argv = process.argv.slice(2)) {
    const { command, options } = parseArgs(argv);
    if (command === 'help') {
        console.log(`FoodDiary WebQuality — pinned Google guidance and explicit evidence
  validate                                     Verify catalogue, cached guides and reviewed mappings offline
  build [--report-dir PATH]                     Build production and record source/output fingerprints
  catalogue [--search TEXT] [--guide ID]         Inspect coverage or a cached guide
  audit [--profile source|contracts|browser|journeys|performance|all]
        [--base-url ORIGIN | --static-root PATH] [--report-dir PATH]
        [--browser chromium|firefox|webkit] [--channel chrome]
        [--routes /,/ru] [--devices mobile,desktop] [--strict] [--complete]
  update --ref REF --output DIRECTORY           Stage upstream changes; accepted files are unchanged
  diff --candidate DIRECTORY                    List changed guides and affected requirements
  adopt --candidate DIRECTORY --note TEXT       Adopt only after requirement source hashes are reviewed
  snapshot --ref REF --output DIRECTORY [--cache ID,ID]  Bootstrap an explicit published snapshot
Exit codes: 0 = no blocking findings in executed checks; 1 = violations/incomplete requested coverage; 2 = invalid configuration or engine failure.
Exit 0 is never a claim of complete guide coverage. Reports include not-run, manual and unreviewed work.`);
        return 0;
    }
    if (command === 'snapshot') {
        if (!options.output || !options.ref) throw new Error('snapshot requires --ref and --output.');
        const output = path.resolve(repositoryRoot, options.output);
        assertSeparateStage(output, snapshotDirectory);
        const snapshot = await buildSnapshot(options.ref, options.cache?.split(',') ?? defaults);
        await saveSnapshot(output, snapshot);
        console.log(`Staged ${snapshot.lock.guides.length} guides at ${snapshot.lock.commit}: ${output}`);
        return 0;
    }
    if (command === 'build') {
        const reportDirectory = path.resolve(repositoryRoot, options['report-dir'] ?? '.artifacts/web-quality/build');
        const receipt = await buildProduction(repositoryRoot, reportDirectory);
        console.log(`Production build verified: ${receipt.outputs}\nReceipt: .artifacts/web-quality/build.json`);
        return 0;
    }
    const lock = await readJson(path.join(snapshotDirectory, 'upstream.lock.json'));
    const policy = await readJson(policyFile);
    if (command === 'validate') {
        await validate(lock, policy);
        console.log(`Valid: ${lock.guides.length} pinned guides, ${policy.requirements.length} reviewed requirements.`);
        return 0;
    }
    if (command === 'audit') return audit(options, lock, policy);
    if (command === 'catalogue') {
        await validateSnapshot(snapshotDirectory, lock);
        if (options.guide) {
            const guide = lock.guides.find(guide => guide.id === options.guide);
            if (!guide) throw new Error(`Unknown guide: ${options.guide}`);
            console.log(
                guide.cache
                    ? await readFile(path.join(snapshotDirectory, guide.cache), 'utf8')
                    : `Not cached: https://github.com/${lock.repository}/blob/${lock.commit}/${guide.path}`,
            );
        } else
            console.log(
                JSON.stringify(
                    coverage(lock, policy).filter(guide => !options.search || `${guide.id} ${guide.category}`.includes(options.search)),
                    null,
                    2,
                ),
            );
        return 0;
    }
    if (command === 'update') {
        if (!options.ref || !options.output) throw new Error('update requires --ref and --output.');
        const output = path.resolve(repositoryRoot, options.output);
        assertSeparateStage(output, snapshotDirectory);
        const next = await buildSnapshot(options.ref, [
            ...lock.guides.filter(guide => guide.cache).map(guide => guide.id),
            ...(options.cache?.split(',') ?? []),
        ]);
        await saveSnapshot(output, next);
        const changes = compareSnapshots(lock, next.lock, policy.requirements);
        await writeJson(path.join(output, 'changes.json'), { from: lock.commit, to: next.lock.commit, changes });
        console.log(JSON.stringify({ from: lock.commit, to: next.lock.commit, changes }, null, 2));
        return 0;
    }
    if (!options.candidate) throw new Error(`${command} requires --candidate.`);
    const candidate = path.resolve(repositoryRoot, options.candidate);
    if (command === 'diff') {
        const next = await readJson(path.join(candidate, 'upstream.lock.json'));
        await validateSnapshot(candidate, next);
        console.log(JSON.stringify(compareSnapshots(lock, next, policy.requirements), null, 2));
    }
    if (command === 'adopt') {
        await adoptSnapshot(
            candidate,
            snapshotDirectory,
            policy,
            (policy, lock, directory) => validate(lock, policy, directory),
            options.note,
        );
        console.log('Adopted reviewed snapshot. Inspect the Git diff and rerun validate, tests and audits.');
    }
    return 0;
}

if (process.argv[1] && pathToFileURL(path.resolve(process.argv[1])).href === import.meta.url) {
    try {
        process.exitCode = await main();
    } catch (error) {
        console.error(`WebQuality error: ${error.message}`);
        process.exitCode = 2;
    }
}
