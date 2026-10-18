export type LocalTool = {
    name: string;
    description: string;
    inputSchema: Record<string, unknown>;
    annotations: { readOnlyHint: boolean };
    execute: (input: unknown) => Promise<unknown>;
};

export type LocalModelContext = {
    registerTool: (tool: LocalTool, options?: { signal: AbortSignal }) => void | Promise<void>;
    unregisterTool?: (name: string) => void;
};

export function localModelContext(pageDocument: Document, development: boolean): LocalModelContext | undefined {
    if (!development || !['localhost', '127.0.0.1', '[::1]'].includes(pageDocument.location.hostname)) {
        return undefined;
    }
    const context = (pageDocument as Document & { modelContext?: LocalModelContext }).modelContext;
    return typeof context?.registerTool === 'function' ? context : undefined;
}
