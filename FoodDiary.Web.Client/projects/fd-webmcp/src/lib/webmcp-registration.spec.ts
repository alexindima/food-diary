import { describe, expect, it, vi } from 'vitest';

import type { WebmcpModelContext, WebmcpTool } from './webmcp.types';
import { registerWebmcpTools } from './webmcp-registration';

const tool = (name: string): WebmcpTool => ({
    name,
    description: name,
    inputSchema: {},
    annotations: { readOnlyHint: true },
    execute: vi.fn<WebmcpTool['execute']>().mockResolvedValue(null),
});

describe('WebMCP registration diagnostics', () => {
    it('reports completion and failure without letting diagnostics break tool cleanup', async () => {
        const states = vi.fn();
        const dispose = registerWebmcpTools({ registerTool: vi.fn() }, [tool('screen')], states);
        await vi.waitFor(() => {
            expect(states).toHaveBeenLastCalledWith({ registration: 'ready', registeredToolCount: 1 });
        });
        dispose();
        expect(states).toHaveBeenLastCalledWith({ registration: 'disposed', registeredToolCount: 0 });
        const failures = vi.fn();
        registerWebmcpTools({ registerTool: vi.fn().mockRejectedValue(new Error('unsupported')) }, [tool('broken')], failures);
        await vi.waitFor(() => {
            expect(failures).toHaveBeenLastCalledWith({ registration: 'failed', registeredToolCount: 0 });
        });
        const cleanup = registerWebmcpTools({ registerTool: vi.fn() }, [tool('safe')], () => {
            throw new Error('diagnostics only');
        });
        expect(cleanup).not.toThrow();
    });
});

describe('WebMCP registration lifecycle', () => {
    it('registers in order with a shared abort signal and disposes once', async () => {
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>();
        const dispose = registerWebmcpTools({ registerTool }, [tool('first'), tool('second')]);
        await vi.waitFor(() => {
            expect(registerTool).toHaveBeenCalledTimes(2);
        });
        expect(registerTool.mock.calls.map(([item]) => item.name)).toEqual(['first', 'second']);
        const signal = registerTool.mock.calls[0][1]?.signal;
        expect(signal).toBe(registerTool.mock.calls[1][1]?.signal);
        expect(signal?.aborted).toBe(false);
        dispose();
        dispose();
        expect(signal?.aborted).toBe(true);
    });

    it('aborts partial registration on failure without attempting later tools', async () => {
        const registerTool = vi
            .fn<WebmcpModelContext['registerTool']>()
            .mockResolvedValueOnce()
            .mockRejectedValueOnce(new Error('unsupported'));
        registerWebmcpTools({ registerTool }, [tool('first'), tool('second'), tool('third')]);
        await vi.waitFor(() => {
            expect(registerTool.mock.calls[0][1]?.signal?.aborted).toBe(true);
        });
        expect(registerTool).toHaveBeenCalledTimes(2);
        expect(registerTool.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });
});

describe('WebMCP registration cleanup', () => {
    it('stops a pending registration on session cleanup before further tools are published', async () => {
        let resume: () => void = () => {};
        const pending = new Promise<void>(resolve => {
            resume = resolve;
        });
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>().mockReturnValue(pending);
        const dispose = registerWebmcpTools({ registerTool }, [tool('first'), tool('second')]);
        dispose();
        resume();
        await pending;
        expect(registerTool).toHaveBeenCalledOnce();
        expect(registerTool.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });

    it('ignores a late registration failure after session cleanup', async () => {
        let rejectRegistration: (reason: Error) => void = () => {};
        const pending = new Promise<void>((_, reject) => {
            rejectRegistration = reject;
        });
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>().mockReturnValue(pending);
        const states = vi.fn();
        const dispose = registerWebmcpTools({ registerTool }, [tool('first'), tool('second')], states);

        dispose();
        expect(registerTool.mock.calls[0][1]?.signal?.aborted).toBe(true);
        rejectRegistration(new Error('session already closed'));
        // Let the rejected registration and its diagnostic callbacks settle.
        await new Promise<void>(resolve => {
            setTimeout(resolve, 0);
        });
        dispose();

        expect(registerTool).toHaveBeenCalledOnce();
        expect(states.mock.calls).toEqual([
            [{ registration: 'registering', registeredToolCount: 0 }],
            [{ registration: 'disposed', registeredToolCount: 0 }],
        ]);
    });

    it('does not abort the replacement session registration when an old session closes', async () => {
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>();
        const context = { registerTool };
        const oldDispose = registerWebmcpTools(context, [tool('screen')]);
        await vi.waitFor(() => {
            expect(registerTool).toHaveBeenCalledOnce();
        });
        const newDispose = registerWebmcpTools(context, [tool('screen')]);
        oldDispose();
        expect(registerTool.mock.calls[0][1]?.signal?.aborted).toBe(true);
        expect(registerTool.mock.calls[1][1]?.signal?.aborted).toBe(false);
        newDispose();
        expect(registerTool.mock.calls[1][1]?.signal?.aborted).toBe(true);
    });

    it('disposes without requiring any nonstandard cleanup API', () => {
        expect(registerWebmcpTools({ registerTool: vi.fn() }, [tool('screen')])).not.toThrow();
    });
});
