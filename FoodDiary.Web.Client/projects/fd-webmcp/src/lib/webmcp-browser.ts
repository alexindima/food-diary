import type { WebmcpModelContext } from './webmcp.types';

export type WebmcpDocument = { location?: { hostname: string }; modelContext?: WebmcpModelContext };

/** SSR documents may expose a defaultView without the browser Crypto API. */
export function createWebmcpContextId(pageDocument: { defaultView?: { crypto?: { randomUUID?: () => string } } | null }): string | null {
    return pageDocument.defaultView?.crypto?.randomUUID?.() ?? null;
}

export type WebmcpAvailability = {
    available: boolean;
    reason: 'ready' | 'production-build' | 'non-local-host' | 'browser-api-unavailable';
    development: boolean;
    localHost: boolean;
    browserApi: boolean;
};

export function inspectWebmcpAvailability(pageDocument: WebmcpDocument, development: boolean): WebmcpAvailability {
    const localHost = ['localhost', '127.0.0.1', '[::1]'].includes(pageDocument.location?.hostname ?? '');
    const browserApi = typeof pageDocument.modelContext?.registerTool === 'function';
    const reason = !development ? 'production-build' : !localHost ? 'non-local-host' : !browserApi ? 'browser-api-unavailable' : 'ready';
    return { available: reason === 'ready', reason, development, localHost, browserApi };
}

/** Safe local diagnostics remain readable through DOM when no Site tools bridge exists. */
export function publishWebmcpDiagnostics(
    pageDocument: Document,
    availability: WebmcpAvailability,
    state: { authenticated: boolean; registration: string; registeredToolCount: number },
): void {
    if (!availability.development || !availability.localHost) {
        return;
    }
    const selector = 'meta[name="fooddiary-webmcp"]';
    let meta = pageDocument.querySelector<HTMLMetaElement>(selector);
    if (meta === null) {
        meta = pageDocument.createElement('meta');
        meta.name = 'fooddiary-webmcp';
        pageDocument.head.append(meta);
    }
    meta.content = JSON.stringify({ schemaVersion: 1, ...availability, ...state, updatedAtUtc: new Date().toISOString() });
}

/** The caller supplies its injected DOCUMENT and development mode; no browser globals or polyfill. */
export function localWebmcpContext(pageDocument: WebmcpDocument, development: boolean): WebmcpModelContext | undefined {
    return inspectWebmcpAvailability(pageDocument, development).available ? pageDocument.modelContext : undefined;
}
