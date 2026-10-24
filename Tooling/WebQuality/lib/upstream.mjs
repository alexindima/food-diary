import { readFile, mkdir, writeFile, copyFile } from 'node:fs/promises';
import path from 'node:path';
import { gitBlob, readJson, within, writeJson } from './files.mjs';

export const REPOSITORY = 'GoogleChrome/modern-web-guidance';
const prefix = 'skills/modern-web-guidance/guides/';
const shaPattern = /^[a-f0-9]{40}$/u;

export function assertSeparateStage(candidate, accepted) {
    const contains = (root, target) => {
        const relative = path.relative(path.resolve(root), path.resolve(target));
        return !relative || (!relative.startsWith('..') && !path.isAbsolute(relative));
    };
    if (contains(candidate, accepted) || contains(accepted, candidate))
        throw new Error('Candidate and accepted source directories must not overlap.');
}

async function download(url, fetcher) {
    const response = await fetcher(url, { signal: AbortSignal.timeout(30_000), headers: { 'User-Agent': 'FoodDiary-WebQuality' } });
    if (!response.ok) throw new Error(`Upstream HTTP ${response.status}: ${url}`);
    const bytes = await response.arrayBuffer();
    if (bytes.byteLength > 8 * 1024 * 1024) throw new Error('Upstream response exceeds 8 MiB.');
    return Buffer.from(bytes).toString('utf8');
}

// Only published Markdown and metadata are consumed. Upstream code/skills are never installed or executed.
export async function buildSnapshot(ref, cacheIds, fetcher = fetch) {
    if (!ref || /[\s?#]/u.test(ref)) throw new Error('An explicit upstream ref is required.');
    const revision = JSON.parse(await download(`https://api.github.com/repos/${REPOSITORY}/commits/${encodeURIComponent(ref)}`, fetcher));
    if (!shaPattern.test(revision.sha) || !shaPattern.test(revision.commit?.tree?.sha))
        throw new Error('Cannot resolve an immutable upstream commit/tree pair.');
    const tree = JSON.parse(
        await download(`https://api.github.com/repos/${REPOSITORY}/git/trees/${revision.commit.tree.sha}?recursive=1`, fetcher),
    );
    if (tree.sha !== revision.commit.tree.sha || tree.truncated || !Array.isArray(tree.tree))
        throw new Error('Invalid or truncated upstream Git tree.');
    const guides = tree.tree
        .filter(entry => entry.type === 'blob' && entry.path.startsWith(prefix) && entry.path.endsWith('.md'))
        .map(entry => ({
            id: path.posix.basename(entry.path, '.md'),
            category: entry.path.slice(prefix.length).split('/')[0],
            path: entry.path,
            blob: entry.sha,
        }))
        .sort((a, b) => a.id.localeCompare(b.id));
    if (!guides.length || new Set(guides.map(guide => guide.id)).size !== guides.length)
        throw new Error('Empty or ambiguous upstream guide catalogue.');
    const texts = new Map();
    for (const id of [...new Set(cacheIds)].sort()) {
        const guide = guides.find(item => item.id === id);
        if (!guide) continue; // Removed mapped guides remain explicit in the diff; adoption will reject them.
        const text = await download(`https://raw.githubusercontent.com/${REPOSITORY}/${revision.sha}/${guide.path}`, fetcher);
        if (gitBlob(text) !== guide.blob) throw new Error(`Upstream blob mismatch: ${id}`);
        guide.cache = `sources/${guide.blob}.md`;
        texts.set(guide.cache, text);
    }
    const packageText = await download(`https://raw.githubusercontent.com/${REPOSITORY}/${revision.sha}/package.json`, fetcher);
    const packageEntry = tree.tree.find(entry => entry.path === 'package.json');
    if (!packageEntry || gitBlob(packageText) !== packageEntry.sha) throw new Error('Upstream package metadata blob mismatch.');
    return {
        lock: {
            schemaVersion: 1,
            repository: REPOSITORY,
            commit: revision.sha,
            tree: tree.sha,
            version: JSON.parse(packageText).version,
            guides,
        },
        texts,
    };
}

export async function saveSnapshot(directory, snapshot) {
    await mkdir(path.join(directory, 'sources'), { recursive: true });
    for (const [file, text] of snapshot.texts) await writeFile(within(directory, file), text, 'utf8');
    await writeJson(path.join(directory, 'upstream.lock.json'), snapshot.lock);
}

export function compareSnapshots(previous, next, requirements) {
    const before = new Map(previous.guides.map(guide => [guide.id, guide]));
    const after = new Map(next.guides.map(guide => [guide.id, guide]));
    return [...new Set([...before.keys(), ...after.keys()])].sort().flatMap(id => {
        const oldGuide = before.get(id);
        const newGuide = after.get(id);
        if (oldGuide?.blob === newGuide?.blob && oldGuide?.path === newGuide?.path) return [];
        return [
            {
                id,
                change: !oldGuide ? 'added' : !newGuide ? 'removed' : 'changed',
                before: oldGuide?.blob ?? null,
                after: newGuide?.blob ?? null,
                requirements: requirements.filter(item => item.source.guide === id).map(item => item.id),
            },
        ];
    });
}

export async function validateSnapshot(directory, lock) {
    if (
        lock.schemaVersion !== 1 ||
        lock.repository !== REPOSITORY ||
        !shaPattern.test(lock.commit) ||
        (lock.tree !== undefined && !shaPattern.test(lock.tree)) ||
        typeof lock.version !== 'string' ||
        !lock.version.trim() ||
        !Array.isArray(lock.guides) ||
        !lock.guides.length
    ) {
        throw new Error('Invalid pinned upstream snapshot.');
    }
    const ids = new Set();
    for (const guide of lock.guides) {
        if (
            !/^[a-z0-9-]+$/u.test(guide.id) ||
            ids.has(guide.id) ||
            !shaPattern.test(guide.blob) ||
            !guide.path.startsWith(prefix) ||
            !guide.path.endsWith(`/${guide.id}.md`) ||
            guide.path.includes('..')
        )
            throw new Error(`Invalid upstream guide: ${guide.id}`);
        ids.add(guide.id);
        if (guide.cache) {
            if (guide.cache !== `sources/${guide.blob}.md`) throw new Error(`Invalid cache path: ${guide.id}`);
            const text = await readFile(within(directory, guide.cache), 'utf8');
            if (gitBlob(text) !== guide.blob) throw new Error(`Cached upstream content changed: ${guide.id}`);
        }
    }
}

export async function adoptSnapshot(candidateDirectory, acceptedDirectory, policy, validatePolicy, note) {
    if (!note?.trim()) throw new Error('Adoption requires a review note.');
    assertSeparateStage(candidateDirectory, acceptedDirectory);
    const lock = await readJson(path.join(candidateDirectory, 'upstream.lock.json'));
    await validateSnapshot(candidateDirectory, lock);
    await validatePolicy(policy, lock, candidateDirectory);
    // Content-addressed files go first; the lock is atomically replaced last.
    await mkdir(path.join(acceptedDirectory, 'sources'), { recursive: true });
    for (const guide of lock.guides.filter(item => item.cache))
        await copyFile(within(candidateDirectory, guide.cache), within(acceptedDirectory, guide.cache));
    await writeJson(path.join(acceptedDirectory, 'review.json'), { commit: lock.commit, policyReviewedAt: new Date().toISOString(), note });
    await writeJson(path.join(acceptedDirectory, 'upstream.lock.json'), lock);
}
