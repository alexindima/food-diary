import type { WebmcpOperation, WebmcpTool } from './webmcp.types';

export type WebmcpExecutionPolicy = {
    requireSession: () => void;
    errorMessage: (error: unknown) => string;
    failureDescription: string;
};

/** One instance per application adapter. Context reads remain available during a pending mutation. */
export class WebmcpExecutor {
    private busy = false;

    public tool(operation: WebmcpOperation, policy: WebmcpExecutionPolicy): WebmcpTool {
        const { run: _run, ...definition } = operation;
        const executeAsync = async (input: unknown): Promise<unknown> => this.executeAsync(operation, policy, input);
        return {
            ...definition,
            description: `${definition.description} ${policy.failureDescription}`,
            execute: executeAsync,
        };
    }

    private async executeAsync(operation: WebmcpOperation, policy: WebmcpExecutionPolicy, input: unknown): Promise<unknown> {
        const changesScreen = !operation.annotations.readOnlyHint;
        try {
            policy.requireSession();
            if (changesScreen && this.busy) {
                return { ok: false, error: 'Another development tool is running.' };
            }
            if (changesScreen) {
                this.busy = true;
            }
            try {
                const result = await operation.run(input);
                policy.requireSession();
                return result;
            } finally {
                if (changesScreen) {
                    this.busy = false;
                }
            }
        } catch (error: unknown) {
            return { ok: false, error: policy.errorMessage(error) };
        }
    }
}
