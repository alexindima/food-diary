import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { within } from './files.mjs';

export const engines = new Set(['source', 'browser', 'journeys', 'performance', 'contracts', 'manual']);
export const statuses = new Set(['pass', 'fail', 'not-run', 'manual', 'not-applicable', 'exception', 'error']);

export async function validatePolicy(policy, lock, snapshotDirectory) {
    if (policy.schemaVersion !== 1 || !Array.isArray(policy.requirements) || !policy.requirements.length || !Array.isArray(policy.scopes))
        throw new Error('Invalid quality policy.');
    const scopes = new Set(policy.scopes.map(scope => scope.id));
    const repositoryRoot = path.resolve(snapshotDirectory, '../../..');
    for (const file of policy.additionalSources ?? []) await stat(within(repositoryRoot, file));
    if (scopes.size !== policy.scopes.length || !scopes.size) throw new Error('Empty or duplicate source scopes.');
    for (const scope of policy.scopes) {
        within('/quality-root', scope.path);
        if (!/^[a-z-]+$/u.test(scope.id)) throw new Error('Invalid scope ID.');
    }
    const ids = new Set();
    for (const requirement of policy.requirements) {
        const { id, source, engine, check, severity, statement } = requirement;
        if (!/^[a-z0-9-]+$/u.test(id) || ids.has(id)) throw new Error(`Invalid or duplicate requirement: ${id}`);
        ids.add(id);
        const guide = lock.guides.find(item => item.id === source?.guide);
        if (!guide?.cache || source.reviewedBlob !== guide.blob) throw new Error(`Source review is missing or stale: ${id}`);
        const text = await readFile(within(snapshotDirectory, guide.cache), 'utf8');
        if (!source.anchor || !text.toLowerCase().includes(source.anchor.toLowerCase())) throw new Error(`Source anchor is missing: ${id}`);
        if (!engines.has(engine) || !check || !statement?.trim() || !['error', 'warning'].includes(severity))
            throw new Error(`Incomplete requirement: ${id}`);
        if (!Array.isArray(requirement.scopes) || !requirement.scopes.length || requirement.scopes.some(scope => !scopes.has(scope)))
            throw new Error(`Invalid requirement scopes: ${id}`);
        if (engine === 'manual' && !requirement.procedure?.trim()) throw new Error(`Manual procedure required: ${id}`);
    }
    for (const exception of policy.exceptions ?? []) {
        if (
            !ids.has(exception.requirement) ||
            !exception.reason?.trim() ||
            !exception.owner?.trim() ||
            !exception.scope?.trim() ||
            !/^\d{4}-\d{2}-\d{2}$/u.test(exception.expires) ||
            !Number.isFinite(Date.parse(exception.expires)) ||
            new Date(exception.expires).toISOString().slice(0, 10) !== exception.expires
        )
            throw new Error('Incomplete exception.');
        if (exception.expires < new Date().toISOString().slice(0, 10)) throw new Error(`Expired exception: ${exception.requirement}`);
    }
    const reviewedGuides = new Set();
    for (const review of policy.guideReviews ?? []) {
        const guide = lock.guides.find(item => item.id === review.guide);
        if (
            !guide ||
            reviewedGuides.has(review.guide) ||
            review.reviewedBlob !== guide.blob ||
            !review.reason?.trim() ||
            !['partial', 'reviewed', 'not-applicable'].includes(review.status)
        )
            throw new Error(`Invalid or stale guide review: ${review.guide}`);
        if (review.status === 'not-applicable' && policy.requirements.some(item => item.source.guide === review.guide))
            throw new Error(`Mapped guide cannot be declared inapplicable: ${review.guide}`);
        reviewedGuides.add(review.guide);
        if (review.requirements && (!review.requirements.length || review.requirements.some(id => !ids.has(id))))
            throw new Error(`Guide review references missing assertions: ${review.guide}`);
        if (review.whenAbsent) {
            if (!review.whenAbsent.reason?.trim() || !review.whenAbsent.patterns?.length)
                throw new Error(`Missing applicability probe: ${review.guide}`);
            for (const pattern of review.whenAbsent.patterns) {
                const probe = new RegExp(pattern, 'gmu');
                if (probe.test('')) throw new Error(`Empty applicability probe: ${review.guide}`);
                if (policy.reviewMode === 'all-guides' && !review.whenAbsent.examples?.some(example => new RegExp(pattern, 'mu').test(example)))
                    throw new Error(`Applicability probe lacks a positive fixture: ${review.guide}`);
            }
        }
        if (policy.reviewMode === 'all-guides') {
            if (!guide.cache || !review.anchor?.trim() || !review.requirements?.length || !review.evidence?.length || review.status === 'partial')
                throw new Error(`Incomplete full-catalogue decision: ${review.guide}`);
            const body = await readFile(within(snapshotDirectory, guide.cache), 'utf8');
            if (!body.includes(review.anchor)) throw new Error(`Guide review anchor missing: ${review.guide}`);
            for (const file of review.evidence) await stat(within(repositoryRoot, file));
            if (review.status === 'not-applicable' && !review.whenAbsent)
                throw new Error(`Inapplicability must be executable: ${review.guide}`);
            if (review.whenAbsent && !review.requirements.some(id => policy.requirements.some(item => item.id === id && item.engine === 'source' && item.check === 'source:guide-applicability')))
                throw new Error(`Applicability review is not connected to its executable guard: ${review.guide}`);
        }
    }
    if (policy.reviewMode === 'all-guides' && reviewedGuides.size !== lock.guides.length)
        throw new Error('Every pinned guide requires an explicit current review decision.');
}

export function initialResults(policy) {
    return policy.requirements.map(requirement => ({
        requirement: requirement.id,
        engine: requirement.engine,
        status: requirement.engine === 'manual' ? 'manual' : 'not-run',
        severity: requirement.severity,
        findings: [],
        detail: requirement.engine === 'manual' ? requirement.procedure : 'This engine has not run.',
        observations: 0,
    }));
}

export function mergeResults(policy, initial, executed) {
    const known = new Map(initial.map(result => [result.requirement, result]));
    const seen = new Set();
    for (const result of executed) {
        const requirement = policy.requirements.find(item => item.id === result.requirement);
        if (!requirement || seen.has(result.requirement) || !statuses.has(result.status) || result.engine !== requirement.engine)
            throw new Error('Invalid or duplicate engine evidence.');
        if (['pass', 'not-applicable'].includes(result.status) && !(result.observations > 0))
            throw new Error(`Empty evidence cannot pass: ${result.requirement}`);
        if (result.status === 'fail' && !result.findings?.length)
            throw new Error(`Failed requirement has no findings: ${result.requirement}`);
        seen.add(result.requirement);
        known.set(result.requirement, { ...result, severity: requirement.severity });
    }
    return [...known.values()].map(result => {
        const exception = (policy.exceptions ?? []).find(
            item =>
                item.requirement === result.requirement &&
                result.findings?.length &&
                result.findings.every(finding => finding.file === item.scope || finding.case === item.scope),
        );
        return exception && result.status === 'fail' ? { ...result, status: 'exception', exception } : result;
    });
}

export function exitCode(results, { strict = false, complete = false } = {}) {
    if (results.some(result => result.status === 'error')) return 2;
    if (results.some(result => result.status === 'fail' && (strict || result.severity === 'error'))) return 1;
    if (complete && results.some(result => ['not-run', 'manual', 'exception'].includes(result.status))) return 1;
    return 0;
}
