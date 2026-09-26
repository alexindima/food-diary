import { DatabaseSync, backup } from 'node:sqlite';
import { mkdirSync, renameSync, rmSync } from 'node:fs';
import { dirname } from 'node:path';
import { pathToFileURL } from 'node:url';

export async function snapshotCodeGraph(sourcePath, destinationPath) {
  mkdirSync(dirname(destinationPath), { recursive: true });
  const temporaryPath = `${destinationPath}.${process.pid}.tmp`;
  const source = new DatabaseSync(sourcePath, { readOnly: true });
  try {
    // SQLite coordinates the database and WAL while other readers checkpoint it.
    await backup(source, temporaryPath);
    renameSync(temporaryPath, destinationPath);
  } finally {
    source.close();
    rmSync(temporaryPath, { force: true });
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await snapshotCodeGraph(process.argv[2], process.argv[3]);
}
