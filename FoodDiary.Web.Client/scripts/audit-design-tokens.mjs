import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';

const workspaceRoot = process.cwd();
const tokenFile = path.join(workspaceRoot, 'src', 'styles', 'design-tokens.scss');
const scanRoots = ['src', 'projects'].map(root => path.join(workspaceRoot, root));
const supportedExtensions = new Set(['.scss', '.css', '.html', '.ts']);
const declarationPattern = /(--fd-[a-z0-9-]+)\s*:/gi;
const usagePattern = /var\(\s*(--fd-[a-z0-9-]+)(?=\s*[,)]\s*)/gi;

const tokenSource = await readFile(tokenFile, 'utf8');
const declarations = collectMatches(tokenSource, declarationPattern);
const allDeclarations = new Set(declarations);
const usages = new Map();

for (const root of scanRoots) {
    for (const file of await collectFiles(root)) {
        if (path.resolve(file) === path.resolve(tokenFile)) {
            continue;
        }

        const source = await readFile(file, 'utf8');
        for (const token of collectMatches(source, declarationPattern)) {
            allDeclarations.add(token);
        }
        for (const token of collectMatches(source, /\[style\.(--fd-[a-z0-9-]+)\]/gi)) {
            allDeclarations.add(token);
        }
        for (const token of collectMatches(source, usagePattern)) {
            usages.set(token, (usages.get(token) || 0) + 1);
        }
    }
}

const unused = [...declarations].filter(token => !usages.has(token)).sort();
const unknown = [...usages.keys()].filter(token => !allDeclarations.has(token)).sort();
const mostUsed = [...usages.entries()].sort((left, right) => right[1] - left[1]).slice(0, 20);

console.log(`Declared tokens: ${declarations.size}`);
console.log(`Used declared tokens: ${declarations.size - unused.length}`);
console.log(`Unused declared tokens: ${unused.length}`);
console.log(`Unknown token references: ${unknown.length}`);
console.log('\nMost-used tokens:');
for (const [token, count] of mostUsed) {
    console.log(`  ${token}: ${count}`);
}

printList('Unused tokens (review before removal)', unused);
printList('Unknown references (must be defined or corrected)', unknown);

if (unknown.length > 0) {
    process.exitCode = 1;
}

function collectMatches(source, pattern) {
    const matches = new Set();
    pattern.lastIndex = 0;
    for (const match of source.matchAll(pattern)) {
        matches.add(match[1]);
    }
    return matches;
}

async function collectFiles(directory) {
    const entries = await readdir(directory, { withFileTypes: true });
    const files = [];
    for (const entry of entries) {
        const target = path.join(directory, entry.name);
        if (entry.isDirectory()) {
            files.push(...(await collectFiles(target)));
        } else if (supportedExtensions.has(path.extname(entry.name))) {
            files.push(target);
        }
    }
    return files;
}

function printList(title, values) {
    if (values.length === 0) {
        return;
    }

    console.log(`\n${title}:`);
    for (const value of values) {
        console.log(`  ${value}`);
    }
}
