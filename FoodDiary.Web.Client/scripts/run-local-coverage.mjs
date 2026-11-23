import { execFileSync, spawn } from 'node:child_process';
import {
    closeSync,
    existsSync,
    lstatSync,
    mkdirSync,
    mkdtempSync,
    openSync,
    readFileSync,
    readdirSync,
    realpathSync,
    renameSync,
    rmSync,
    writeFileSync,
    writeSync,
} from 'node:fs';
import { basename, dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';

const frontend = fileURLToPath(new URL('..', import.meta.url));
const projects = new Set(['app', 'admin', 'ui-kit', 'tour', 'webmcp']);
const runName = /^run-[\da-z]{6}$/iu;
const maxLogBytes = 4 * 1024 * 1024;
const usage = [
    'npm run test:coverage:local -- <app|admin|ui-kit|tour|webmcp> [--full | --include=GLOB ...]',
    'npm run test:coverage:local -- --plan',
].join('\n');

export function coverageArgs(project, full, include) {
    const args = ['run', `test:coverage:${project}`];
    if (!full) {
        args.push('--', '--coverage-reporters=text-summary', '--coverage-reporters=json-summary');
        args.push(...include.map(pattern => `--include=${pattern}`));
    }
    return args;
}

function artifactRoot(create) {
    const repo = realpathSync(join(frontend, '..'));
    const artifacts = join(repo, '.artifacts');
    const managed = join(artifacts, 'local-frontend-coverage');
    for (const path of [artifacts, managed]) {
        if (!existsSync(path) && !create) return undefined;
        mkdirSync(path, { recursive: true });
        if (!lstatSync(path).isDirectory() || realpathSync(path) !== path) {
            throw new Error(`Coverage location must be a real directory at ${path}`);
        }
    }
    return managed;
}

function readCompletedRun(path, id) {
    try {
        const marker = join(path, 'run.json');
        if (!lstatSync(marker).isFile()) return undefined;
        const run = JSON.parse(readFileSync(marker, 'utf8'));
        if (
            run.schemaVersion === 1 &&
            run.id === id &&
            projects.has(run.project) &&
            ['summary', 'full'].includes(run.mode) &&
            Number.isInteger(run.exitCode) &&
            Number.isFinite(Date.parse(run.finishedAt))
        ) {
            return { ...run, path };
        }
    } catch (error) {
        if (error.code !== 'ENOENT' && !(error instanceof SyntaxError)) throw error;
    }
    return undefined;
}

export function planCleanup(root, project) {
    const runs = [];
    for (const entry of readdirSync(root, { withFileTypes: true })) {
        if (!entry.isDirectory() || !runName.test(entry.name)) continue;
        const path = join(root, entry.name);
        if (existsSync(join(path, '.keep'))) continue;
        const run = readCompletedRun(path, entry.name);
        if (run && (!project || run.project === project)) runs.push(run);
    }
    runs.sort((left, right) => Date.parse(right.finishedAt) - Date.parse(left.finishedAt) || right.id.localeCompare(left.id));

    const counts = new Map();
    return runs.filter(run => {
        const bucket = run.mode === 'summary' ? 'summary' : run.exitCode === 0 ? 'full' : 'failed-full';
        const key = `${run.project}:${bucket}`;
        const count = (counts.get(key) ?? 0) + 1;
        counts.set(key, count);
        return count > { summary: 3, full: 2, 'failed-full': 1 }[bucket];
    });
}

export function prune(root, project) {
    for (const run of planCleanup(root, project)) {
        try {
            if (
                !lstatSync(run.path).isDirectory() ||
                realpathSync(run.path) !== run.path ||
                dirname(run.path) !== root ||
                existsSync(join(run.path, '.keep')) ||
                !readCompletedRun(run.path, run.id)
            ) {
                continue;
            }
            rmSync(run.path, { recursive: true });
            console.log(`Removed expired coverage run ${run.path}`);
        } catch (error) {
            if (error.code !== 'ENOENT') console.warn(`Could not rotate coverage run ${run.path}: ${error.message}`);
        }
    }
}

function hasCoverageSummary(reports) {
    try {
        const summary = JSON.parse(readFileSync(join(reports, 'coverage-summary.json'), 'utf8'));
        return ['lines', 'branches', 'functions', 'statements'].every(metric => Number.isInteger(summary?.total?.[metric]?.total));
    } catch (error) {
        if (error.code === 'ENOENT' || error instanceof SyntaxError) return false;
        throw error;
    }
}

async function runCoverage(root, project, full, include) {
    const npm = process.env.npm_execpath;
    if (!npm) throw new Error('Start this command through npm.');
    const startedAt = new Date().toISOString();
    const head = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: frontend, encoding: 'utf8' }).trim();
    const dirty = Boolean(execFileSync('git', ['status', '--porcelain'], { cwd: frontend, encoding: 'utf8' }).trim());
    const path = mkdtempSync(join(root, 'run-'));
    const reports = join(path, 'coverage');
    const args = coverageArgs(project, full, include);
    const source = { schemaVersion: 1, id: basename(path), project, mode: full ? 'full' : 'summary', args, head, dirty, startedAt };
    writeFileSync(join(path, 'started.json'), JSON.stringify({ ...source, pid: process.pid }, null, 2) + '\n', { flag: 'wx' });
    const log = openSync(join(path, 'test.log'), 'wx');
    console.log(`Coverage run: ${path}`);

    let bytes = 0;
    let logError;
    const capture = (stream, chunk) => {
        stream.write(chunk);
        const remaining = maxLogBytes - bytes;
        if (remaining <= 0 || logError) return;
        try {
            bytes += writeSync(log, chunk.subarray(0, remaining));
        } catch (error) {
            logError = error;
        }
    };
    let result;
    try {
        result = await new Promise(resolve => {
            const child = spawn(process.execPath, [npm, ...args], {
                cwd: frontend,
                env: { ...process.env, FOODDIARY_LOCAL_COVERAGE_REPORTS_DIRECTORY: reports },
                stdio: ['inherit', 'pipe', 'pipe'],
            });
            child.stdout.on('data', chunk => capture(process.stdout, chunk));
            child.stderr.on('data', chunk => capture(process.stderr, chunk));
            child.once('error', error => {
                capture(process.stderr, Buffer.from(`${error.message}\n`));
                resolve({ code: 1, signal: null });
            });
            child.once('close', (code, signal) => resolve({ code, signal }));
        });
    } finally {
        closeSync(log);
    }

    const hasSummary = hasCoverageSummary(reports);
    const exitCode = result.code === 0 && hasSummary && !logError ? 0 : result.code || 1;
    if (!hasSummary) console.error('Coverage summary is unavailable. See test.log.');
    if (logError) console.error(`Coverage log could not be saved: ${logError.message}`);
    if (bytes === maxLogBytes) console.warn('Saved coverage log reached its 4 MiB limit.');
    const terminal = {
        ...source,
        finishedAt: new Date().toISOString(),
        exitCode,
        testExitCode: result.code,
        signal: result.signal,
        hasSummary,
    };
    const marker = join(path, 'run.json');
    writeFileSync(`${marker}.tmp`, JSON.stringify(terminal, null, 2) + '\n', { flag: 'wx' });
    renameSync(`${marker}.tmp`, marker);
    prune(root, project);
    return exitCode;
}

async function main(argv) {
    const { values, positionals } = parseArgs({
        args: argv,
        allowPositionals: true,
        options: {
            help: { type: 'boolean' },
            full: { type: 'boolean' },
            plan: { type: 'boolean' },
            include: { type: 'string', multiple: true },
        },
    });
    if (values.help) {
        console.log(usage);
        return 0;
    }
    if (values.plan && !positionals.length && !values.full && !values.include) {
        const root = artifactRoot(false);
        const runs = root ? planCleanup(root) : [];
        if (runs.length === 0) console.log('No managed coverage runs to rotate.');
        for (const run of runs) console.log(run.path);
        return 0;
    }
    const [project] = positionals;
    if (!projects.has(project) || positionals.length !== 1 || values.plan || (values.full && values.include)) throw new Error(usage);
    return runCoverage(artifactRoot(true), project, values.full ?? false, values.include ?? []);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    main(process.argv.slice(2)).then(
        code => (process.exitCode = code),
        error => {
            console.error(error.message);
            process.exitCode = 1;
        },
    );
}
