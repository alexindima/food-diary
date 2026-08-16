import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';

const REQUEST_TIMEOUT_MS = 15_000;
const server = spawn(process.execPath, ['./node_modules/@angular/cli/bin/ng.js', 'mcp', '--read-only', '--local-only'], {
    cwd: process.cwd(),
    stdio: ['pipe', 'pipe', 'inherit'],
});
const responses = new Map();
const output = createInterface({ input: server.stdout });

output.on('line', line => {
    const message = JSON.parse(line);
    if (message.id !== undefined) {
        responses.get(message.id)?.(message);
    }
});

function send(message) {
    server.stdin.write(`${JSON.stringify(message)}\n`);
}

function request(id, method, params) {
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => {
            responses.delete(id);
            reject(new Error(`Angular MCP request '${method}' timed out.`));
        }, REQUEST_TIMEOUT_MS);
        responses.set(id, message => {
            clearTimeout(timeout);
            responses.delete(id);
            if (message.error !== undefined) {
                reject(new Error(`Angular MCP request '${method}' failed: ${JSON.stringify(message.error)}`));
                return;
            }
            resolve(message.result);
        });
        send({ jsonrpc: '2.0', id, method, params });
    });
}

function assertContentResponse(name, result) {
    if (!Array.isArray(result?.content) || result.content.length === 0) {
        throw new Error(`Angular MCP tool '${name}' returned an invalid content response.`);
    }
}

try {
    const initialization = await request(1, 'initialize', {
        protocolVersion: '2025-06-18',
        capabilities: {},
        clientInfo: { name: 'fooddiary-angular-mcp-smoke', version: '1.0.0' },
    });
    if (initialization?.serverInfo?.name === undefined) {
        throw new Error('Angular MCP returned an invalid initialize response.');
    }

    send({ jsonrpc: '2.0', method: 'notifications/initialized', params: {} });
    assertContentResponse('list_projects', await request(2, 'tools/call', { name: 'list_projects', arguments: {} }));
    assertContentResponse(
        'get_best_practices',
        await request(3, 'tools/call', { name: 'get_best_practices', arguments: { workspacePath: process.cwd() } }),
    );
    console.log(`Angular MCP ${initialization.serverInfo.version}: response shapes are valid.`);
} finally {
    output.close();
    server.stdin.end();
    server.kill();
}
