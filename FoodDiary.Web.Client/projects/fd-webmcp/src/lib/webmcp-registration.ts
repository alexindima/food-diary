import type { WebmcpModelContext, WebmcpTool } from './webmcp.types';

export type WebmcpRegistrationState = { registration: 'registering' | 'ready' | 'failed' | 'disposed'; registeredToolCount: number };

export function registerWebmcpTools(
    context: WebmcpModelContext,
    tools: readonly WebmcpTool[],
    onState?: (state: WebmcpRegistrationState) => void,
): () => void {
    const controller = new AbortController();
    const names = new Set<string>();
    let disposed = false;
    const report = (registration: WebmcpRegistrationState['registration'], registeredToolCount: number): void => {
        try {
            onState?.({ registration, registeredToolCount });
        } catch {
            // Optional diagnostics must not affect registration or session cleanup.
        }
    };
    const dispose = (): void => {
        if (disposed) {
            return;
        }
        disposed = true;
        // The browser owns registration removal through the registration signal.
        // WebMCP has no unregisterTool method.
        controller.abort();
        report('disposed', 0);
    };
    report('registering', 0);
    void registerAsync(context, tools, controller.signal, name => {
        names.add(name);
    })
        .then(() => {
            if (!disposed) {
                report('ready', names.size);
            }
        })
        .catch(() => {
            if (!disposed) {
                dispose();
                report('failed', 0);
            }
        });
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
