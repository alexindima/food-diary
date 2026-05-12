import { readFile } from 'node:fs/promises';

const baselinePath = new URL('../.dependency-cruiser-known-violations.json', import.meta.url);
const baselineText = await readFile(baselinePath, 'utf8');
const baseline = JSON.parse(baselineText);

if (!Array.isArray(baseline)) {
    throw new Error('.dependency-cruiser-known-violations.json must contain an array.');
}

if (baseline.length > 0) {
    throw new Error(`Dependency-cruiser baseline must stay empty. Found ${baseline.length} known violation(s).`);
}
