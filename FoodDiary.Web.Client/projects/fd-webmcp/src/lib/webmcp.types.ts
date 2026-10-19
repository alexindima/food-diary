export type WebmcpTool = {
    name: string;
    description: string;
    inputSchema: Record<string, unknown>;
    annotations: { readOnlyHint: boolean };
    execute: (input: unknown) => Promise<unknown>;
};

export type WebmcpOperation = Omit<WebmcpTool, 'execute'> & { run: (input: unknown) => unknown };

export type WebmcpModelContext = {
    registerTool: (tool: WebmcpTool, options?: { signal: AbortSignal }) => void | Promise<void>;
    unregisterTool?: (name: string) => void;
};
