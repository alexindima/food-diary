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

describe('WebMCP registration lifecycle', () => {
    it('registers in order with a shared abort signal and disposes once', async () => {
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>();
        const unregisterTool = vi.fn();
        const dispose = registerWebmcpTools({ registerTool, unregisterTool }, [tool('first'), tool('second')]);
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
        expect(unregisterTool.mock.calls).toEqual([['first'], ['second']]);
    });

    it('aborts partial registration on failure without attempting later tools', async () => {
        const registerTool = vi
            .fn<WebmcpModelContext['registerTool']>()
            .mockResolvedValueOnce()
            .mockRejectedValueOnce(new Error('unsupported'));
        const unregisterTool = vi.fn();
        registerWebmcpTools({ registerTool, unregisterTool }, [tool('first'), tool('second'), tool('third')]);
        await vi.waitFor(() => {
            expect(unregisterTool).toHaveBeenCalledTimes(2);
        });
        expect(registerTool).toHaveBeenCalledTimes(2);
        expect(registerTool.mock.calls[0][1]?.signal.aborted).toBe(true);
    });

    it('stops a pending registration on session cleanup before further tools are published', async () => {
        let resume: () => void = () => {};
        const pending = new Promise<void>(resolve => {
            resume = resolve;
        });
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>().mockReturnValue(pending);
        const unregisterTool = vi.fn();
        const dispose = registerWebmcpTools({ registerTool, unregisterTool }, [tool('first'), tool('second')]);
        dispose();
        resume();
        await pending;
        expect(registerTool).toHaveBeenCalledOnce();
        expect(unregisterTool).toHaveBeenCalledWith('first');
    });

    it('does not let old cleanup unregister a name now owned by a new session', async () => {
        const registerTool = vi.fn<WebmcpModelContext['registerTool']>();
        const unregisterTool = vi.fn();
        const context = { registerTool, unregisterTool };
        const oldDispose = registerWebmcpTools(context, [tool('screen')]);
        await vi.waitFor(() => {
            expect(registerTool).toHaveBeenCalledOnce();
        });
        const newDispose = registerWebmcpTools(context, [tool('screen')]);
        oldDispose();
        expect(unregisterTool).not.toHaveBeenCalled();
        newDispose();
        expect(unregisterTool).toHaveBeenCalledOnce();
    });

    it('keeps logout safe when optional browser cleanup throws or is missing', () => {
        const context = {
            registerTool: vi.fn(),
            unregisterTool: vi.fn().mockImplementation(() => {
                throw new Error('gone');
            }),
        };
        expect(registerWebmcpTools(context, [tool('screen')])).not.toThrow();
        expect(registerWebmcpTools({ registerTool: vi.fn() }, [tool('screen')])).not.toThrow();
    });
});
