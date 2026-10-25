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
        const executeAsync: WebmcpTool['execute'] = async (input, options) => this.executeAsync(operation, policy, input, options);
        return {
            ...definition,
            annotations: { ...definition.annotations, debugging: true },
            description: `${definition.description} ${policy.failureDescription}`,
            execute: executeAsync,
        };
    }

    private async executeAsync(
        operation: WebmcpOperation,
        policy: WebmcpExecutionPolicy,
        input: unknown,
        options?: WebMCP.ToolExecuteCallbackOptions,
    ): Promise<unknown> {
        const changesScreen = !operation.annotations.readOnlyHint;
        try {
            options?.signal.throwIfAborted();
            policy.requireSession();
            if (changesScreen && this.busy) {
                return { ok: false, error: 'Another development tool is running.' };
            }
            if (changesScreen) {
                this.busy = true;
            }
            try {
                const result = await operation.run(input, options);
                options?.signal.throwIfAborted();
                policy.requireSession();
                return result;
            } finally {
                if (changesScreen) {
                    this.busy = false;
                }
            }
        } catch (error: unknown) {
            options?.signal.throwIfAborted();
            return { ok: false, error: policy.errorMessage(error) };
        }
    }
}
