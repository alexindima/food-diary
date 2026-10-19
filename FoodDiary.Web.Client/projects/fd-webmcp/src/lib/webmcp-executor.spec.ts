import { describe, expect, it, vi } from 'vitest';

import type { WebmcpOperation } from './webmcp.types';
import { WebmcpError } from './webmcp-error';
import { type WebmcpExecutionPolicy, WebmcpExecutor } from './webmcp-executor';

const operation = (readOnlyHint: boolean, run: WebmcpOperation['run']): WebmcpOperation => ({
    name: 'owned-tool',
    description: 'Owner description.',
    inputSchema: {},
    annotations: { readOnlyHint },
    run,
});
const policy = (): WebmcpExecutionPolicy => ({
    requireSession: vi.fn(),
    errorMessage: error => (error instanceof WebmcpError ? error.message : 'Operation unavailable.'),
    failureDescription: 'Errors return {ok:false,error}.',
});

function deferred(): { promise: Promise<unknown>; resolve: (value: unknown) => void } {
    let resolve: (value: unknown) => void = () => {};
    const promise = new Promise<unknown>(complete => {
        resolve = complete;
    });
    return { promise, resolve };
}

describe('WebMCP tool execution', () => {
    it('allows reads during a pending mutation while refusing a second mutation', async () => {
        const executor = new WebmcpExecutor();
        const pending = deferred();
        const write = executor.tool(
            operation(false, async () => pending.promise),
            policy(),
        );
        const read = executor.tool(
            operation(true, () => ({ loading: true })),
            policy(),
        );
        const running = write.execute({});
        expect(await write.execute({})).toEqual({ ok: false, error: 'Another development tool is running.' });
        expect(await read.execute({})).toEqual({ loading: true });
        pending.resolve({ prepared: true });
        expect(await running).toEqual({ prepared: true });
    });

    it('checks the owner session before execution and after an asynchronous result', async () => {
        const executor = new WebmcpExecutor();
        const pending = deferred();
        let valid = true;
        const sessionPolicy = {
            ...policy(),
            requireSession: (): void => {
                if (!valid) {
                    throw new Error('private session value');
                }
            },
        };
        const run = vi.fn().mockReturnValue(pending.promise);
        const wrapped = executor.tool(operation(false, run), sessionPolicy);
        const running = wrapped.execute({});
        valid = false;
        pending.resolve({ privateResult: true });
        expect(await running).toEqual({ ok: false, error: 'Operation unavailable.' });
        expect(await wrapped.execute({})).toEqual({ ok: false, error: 'Operation unavailable.' });
        expect(run).toHaveBeenCalledOnce();
    });

    it('releases the mutation lock after errors and leaves safe error policy with the owner', async () => {
        const executor = new WebmcpExecutor();
        const wrapped = executor.tool(
            operation(false, () => {
                throw new WebmcpError('Unsaved draft exists.');
            }),
            policy(),
        );
        expect(await wrapped.execute({})).toEqual({ ok: false, error: 'Unsaved draft exists.' });
        expect(
            await executor
                .tool(
                    operation(false, () => 'ready'),
                    policy(),
                )
                .execute({}),
        ).toBe('ready');
        expect(wrapped.description).toBe('Owner description. Errors return {ok:false,error}.');
    });

    it('isolates locks between application adapter instances', async () => {
        const pending = deferred();
        const running = new WebmcpExecutor()
            .tool(
                operation(false, async () => pending.promise),
                policy(),
            )
            .execute({});
        expect(
            await new WebmcpExecutor()
                .tool(
                    operation(false, () => 'admin-ready'),
                    policy(),
                )
                .execute({}),
        ).toBe('admin-ready');
        pending.resolve(null);
        await running;
    });
});
