import type {} from 'webmcp-types';

export type WebmcpTool = Omit<WebMCP.ModelContextTool, 'inputSchema' | 'annotations' | 'execute'> & {
    inputSchema: Record<string, unknown>;
    annotations: WebMCP.ToolAnnotations & { readOnlyHint: boolean };
    execute: (input: unknown, options?: WebMCP.ToolExecuteCallbackOptions) => Promise<unknown>;
};

export type WebmcpOperation = Omit<WebmcpTool, 'execute'> & {
    run: (input: unknown, options?: WebMCP.ToolExecuteCallbackOptions) => unknown;
};

export type WebmcpModelContext = {
    registerTool: (tool: WebmcpTool, options?: WebMCP.ModelContextRegisterToolOptions) => void | Promise<void>;
};
