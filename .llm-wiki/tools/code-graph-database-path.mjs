import { toNamespacedPath } from 'node:path';

// SQLite's Windows VFS needs the namespace explicitly for deep fixture paths;
// Node's filesystem APIs add it themselves, so use normal paths for metadata.
export const sqliteDatabasePath = path => toNamespacedPath(path);
