import type { WebmcpModelContext } from './webmcp.types';

export type WebmcpDocument = { location?: { hostname: string }; modelContext?: WebmcpModelContext };

/** The caller supplies its injected DOCUMENT and development mode; no browser globals or polyfill. */
export function localWebmcpContext(pageDocument: WebmcpDocument, development: boolean): WebmcpModelContext | undefined {
    if (!development || !['localhost', '127.0.0.1', '[::1]'].includes(pageDocument.location?.hostname ?? '')) {
        return undefined;
    }
    const context = pageDocument.modelContext;
    return typeof context?.registerTool === 'function' ? context : undefined;
}
