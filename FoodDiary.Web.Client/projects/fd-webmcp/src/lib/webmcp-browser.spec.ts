import { describe, expect, it, vi } from 'vitest';

import { localWebmcpContext, type WebmcpDocument } from './webmcp-browser';

describe('local WebMCP browser boundary', () => {
    const context = { registerTool: vi.fn() };
    const pageDocument = (hostname: string, supported = true): WebmcpDocument => ({
        location: { hostname },
        modelContext: supported ? context : undefined,
    });

    it.each(['localhost', '127.0.0.1', '[::1]'])('accepts supported local development at %s', hostname => {
        expect(localWebmcpContext(pageDocument(hostname), true)).toBe(context);
    });

    it('does not expose tools in production mode or on remote/lookalike hosts', () => {
        expect(localWebmcpContext(pageDocument('localhost'), false)).toBeUndefined();
        for (const hostname of ['fooddiary.club', 'localhost.example.test', '127.0.0.1.example.test', '192.168.1.1']) {
            expect(localWebmcpContext(pageDocument(hostname), true)).toBeUndefined();
        }
    });

    it('tolerates unavailable browser APIs and a server document without location', () => {
        expect(localWebmcpContext(pageDocument('localhost', false), true)).toBeUndefined();
        expect(localWebmcpContext({}, true)).toBeUndefined();
    });
});
