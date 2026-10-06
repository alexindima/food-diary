import { createHash } from 'node:crypto';
import { stat, readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

// Read independently, but publish in Git inventory order: that order also
// determines database ids and extraction batches. All state belongs to this pass.
export async function collectGraphInputs({
  repositoryRoot, paths, previousFiles, dirtyPaths, force = false,
  skipRead = () => false, concurrency = 8, fileSystem = { stat, readFile },
}) {
  if (!Number.isInteger(concurrency) || concurrency < 1) throw new Error('Graph input concurrency must be positive.');
  const inventory = [...new Set(paths)];
  const records = new Array(inventory.length);
  let next = 0;
  let failed = false;
  let failure;
  const workers = Array.from({ length: Math.min(concurrency, inventory.length) }, async () => {
    while (!failed) {
      const index = next++;
      if (index >= inventory.length) return;
      const path = inventory[index];
      const absolutePath = resolve(repositoryRoot, path);
      try {
        let fileStat;
        try { fileStat = await fileSystem.stat(absolutePath); } catch (error) {
          if (error.code === 'ENOENT' || error.code === 'ENOTDIR') continue;
          throw error;
        }
        const record = { path, scanned: 0, unchanged: 0, verifiedDirtyFiles: 0 };
        records[index] = record;
        if (skipRead(path)) continue;
        const prior = previousFiles.get(path);
        const metadataMatches = prior && prior.size === fileStat.size && Math.abs(prior.mtime_ms - fileStat.mtimeMs) < 0.001;
        if (!force && metadataMatches && !dirtyPaths.has(path)) {
          record.unchanged = 1;
          continue;
        }
        const text = await fileSystem.readFile(absolutePath, 'utf8');
        const contentHash = createHash('sha256').update(text).digest('hex');
        if (dirtyPaths.has(path)) record.verifiedDirtyFiles = 1;
        if (!force && prior && prior.content_hash === contentHash) {
          record.unchanged = 1;
          if (!metadataMatches) {
            record.scanned = 1;
            record.candidate = { path, stat: fileStat, prior, text: null, contentHash, metadataOnly: true };
          }
          continue;
        }
        record.scanned = 1;
        record.candidate = { path, stat: fileStat, prior, text, contentHash, metadataOnly: false };
      } catch (error) {
        if (!failed) { failed = true; failure = error; }
      }
    }
  });
  // Drain already dispatched reads before returning or rejecting. The writer
  // retains its lock until every worker settles, including on input failures.
  await Promise.all(workers);
  if (failed) throw failure;
  const present = records.filter(Boolean);
  return {
    knownPaths: new Set(present.map(record => record.path)),
    candidates: present.flatMap(record => record.candidate ? [record.candidate] : []),
    scanned: present.reduce((total, record) => total + record.scanned, 0),
    unchanged: present.reduce((total, record) => total + record.unchanged, 0),
    verifiedDirtyFiles: present.reduce((total, record) => total + record.verifiedDirtyFiles, 0),
  };
}
