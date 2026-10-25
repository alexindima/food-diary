import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export function assertNoDevelopmentRuntime(stats) {
    if (!stats.outputs || !Object.keys(stats.outputs).length) throw new Error('Production bundle metadata is missing.');
    const forbidden = [];
    let contributions = 0;
    for (const [output, metadata] of Object.entries(stats.outputs)) {
        if (!output.endsWith('.js')) continue;
        if (!metadata.inputs) throw new Error(`Missing production input metadata: ${output}`);
        for (const [input, contribution] of Object.entries(metadata.inputs)) {
            if (contribution.bytesInOutput > 0) contributions++;
            const normalized = input.replaceAll('\\', '/');
            if (
                contribution.bytesInOutput > 0 &&
                (/(?:^|\/)src\/app\/composition\/webmcp\/(?!local-webmcp\.providers(?:\.production)?\.ts$)/u.test(normalized) ||
                    /(?:^|\/)admin-webmcp\.service\.ts$/u.test(normalized) ||
                    /(?:^|\/)projects\/fd-webmcp\/src\/(?!lib\/webmcp-screen\.ts$)/u.test(normalized))
            )
                forbidden.push(`${output}: ${input}`);
        }
    }
    if (!contributions) throw new Error('No executed production JavaScript input evidence.');
    if (forbidden.length) throw new Error(`Development WebMCP runtime shipped in production:\n${forbidden.join('\n')}`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    assertNoDevelopmentRuntime(JSON.parse(fs.readFileSync(process.argv[2] ?? 'dist/browser-stats.json', 'utf8')));
    console.log('Production JavaScript excludes development WebMCP implementations.');
}
