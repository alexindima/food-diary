import type { WebmcpModelContext, WebmcpTool } from './webmcp.types';

/** Abort first, then remove only names still owned by this registration. */
const registrations = new WeakMap<WebmcpModelContext, Map<string, symbol>>();

export function registerWebmcpTools(context: WebmcpModelContext, tools: readonly WebmcpTool[]): () => void {
    const controller = new AbortController();
    const owner = Symbol('webmcp-registration');
    const names = new Set<string>();
    const owners = registrations.get(context) ?? new Map<string, symbol>();
    registrations.set(context, owners);
    let disposed = false;
    const dispose = (): void => {
        if (disposed) {
            return;
        }
        disposed = true;
        controller.abort();
        for (const name of names) {
            if (owners.get(name) !== owner) {
                continue;
            }
            owners.delete(name);
            try {
                context.unregisterTool?.(name);
            } catch {
                // Optional browser cleanup must not block logout or app destruction.
            }
        }
    };
    void registerAsync(context, tools, controller.signal, name => {
        names.add(name);
        owners.set(name, owner);
    }).catch(dispose);
    return dispose;
}

async function registerAsync(
    context: WebmcpModelContext,
    tools: readonly WebmcpTool[],
    signal: AbortSignal,
    claim: (name: string) => void,
): Promise<void> {
    for (const tool of tools) {
        if (signal.aborted) {
            return;
        }
        claim(tool.name);
        await context.registerTool(tool, { signal });
    }
}
