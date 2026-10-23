import { describe, expect, it, vi } from 'vitest';

import {
    createWebmcpContextId,
    inspectWebmcpAvailability,
    localWebmcpContext,
    publishWebmcpDiagnostics,
    type WebmcpDocument,
} from './webmcp-browser';

describe('WebMCP context identity', () => {
    it('keeps SSR and unsupported browser identity unknown', () => {
        expect(createWebmcpContextId({})).toBeNull();
        expect(createWebmcpContextId({ defaultView: null })).toBeNull();
        expect(createWebmcpContextId({ defaultView: {} })).toBeNull();
        expect(createWebmcpContextId({ defaultView: { crypto: {} } })).toBeNull();
        expect(createWebmcpContextId({ defaultView: { crypto: { randomUUID: () => 'owned-context' } } })).toBe('owned-context');
    });
});

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

    it('distinguishes browser support, build mode and origin without registering a tool', () => {
        expect(inspectWebmcpAvailability(pageDocument('localhost', false), true).reason).toBe('browser-api-unavailable');
        expect(inspectWebmcpAvailability(pageDocument('localhost'), false).reason).toBe('production-build');
        expect(inspectWebmcpAvailability(pageDocument('fooddiary.club'), true).reason).toBe('non-local-host');
        expect(context.registerTool).not.toHaveBeenCalled();
    });

    it('publishes bounded DOM diagnostics without requiring browser API support', () => {
        const page = document.implementation.createHTMLDocument();
        const availability = inspectWebmcpAvailability(pageDocument('localhost', false), true);
        publishWebmcpDiagnostics(page, availability, { authenticated: false, registration: 'signed-out', registeredToolCount: 0 });
        const meta = page.querySelector<HTMLMetaElement>('meta[name="fooddiary-webmcp"]');
        expect(JSON.parse(meta?.content ?? '{}')).toMatchObject({
            reason: 'browser-api-unavailable',
            authenticated: false,
            registeredToolCount: 0,
        });
        publishWebmcpDiagnostics(page, availability, { authenticated: true, registration: 'not-registered', registeredToolCount: 0 });
        expect(page.querySelectorAll('meta[name="fooddiary-webmcp"]')).toHaveLength(1);
        const updated: unknown = JSON.parse(meta?.content ?? '{}');
        expect(updated).toMatchObject({ authenticated: true });
        const production = document.implementation.createHTMLDocument();
        publishWebmcpDiagnostics(production, inspectWebmcpAvailability(pageDocument('localhost'), false), {
            authenticated: true,
            registration: 'ready',
            registeredToolCount: 4,
        });
        expect(production.querySelector('meta[name="fooddiary-webmcp"]')).toBeNull();
    });
});
