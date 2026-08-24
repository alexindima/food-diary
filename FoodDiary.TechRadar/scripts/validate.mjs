import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = path.resolve(projectRoot, '..');
const catalog = JSON.parse(fs.readFileSync(path.join(projectRoot, 'catalog/radar.json'), 'utf8'));
const errors = [];
const ids = new Set();
for (const entry of catalog.entries) {
  if (ids.has(entry.id)) errors.push(`Duplicate id: ${entry.id}`);
  ids.add(entry.id);
  if (!Number.isInteger(entry.quadrant) || entry.quadrant < 0 || entry.quadrant > 3) errors.push(`${entry.id}: quadrant must be 0..3`);
  if (!Number.isInteger(entry.ring) || entry.ring < 0 || entry.ring > 3) errors.push(`${entry.id}: ring must be 0..3`);
  if (!entry.description || !entry.reviewedAt) errors.push(`${entry.id}: description and reviewedAt are required`);
  for (const evidence of entry.evidence ?? []) {
    if (!fs.existsSync(path.join(repoRoot, evidence.path))) errors.push(`${entry.id}: missing evidence ${evidence.path}`);
  }
}
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log(`Validated ${catalog.entries.length} curated radar entries and their evidence.`);
