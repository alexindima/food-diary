import { InjectionToken } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { connectWebmcpScreen, type WebmcpScreenHost, type WebmcpScreenState } from './webmcp-screen';
import { WebmcpScreenRegistry } from './webmcp-screen-registry';

describe('WebMCP screen connection', () => {
    it('publishes through the application host and detaches when its injection context is destroyed', () => {
        const host = new InjectionToken<WebmcpScreenHost<WebmcpScreenState>>('screen host');
        const screens = new WebmcpScreenRegistry();
        TestBed.configureTestingModule({ providers: [{ provide: host, useValue: screens }] });

        TestBed.runInInjectionContext(() => {
            connectWebmcpScreen(host, { kind: 'products', read: () => ({ query: 'apple' }) });
        });
        expect(screens.read()).toMatchObject({ kind: 'products', data: { query: 'apple' } });

        TestBed.resetTestingModule();
        expect(screens.read()).toBeNull();
    });

    it('leaves screens untouched when the application does not provide the optional host', () => {
        const host = new InjectionToken<WebmcpScreenHost<WebmcpScreenState>>('missing screen host');
        const read = vi.fn();

        expect(() => {
            TestBed.runInInjectionContext(() => {
                connectWebmcpScreen(host, { kind: 'products', read });
            });
        }).not.toThrow();
        expect(read).not.toHaveBeenCalled();
    });
});
