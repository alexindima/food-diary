import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import type { WebmcpModelContext, WebmcpTool } from 'fd-webmcp';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { AdminAuthService } from '../../features/admin-auth/lib/admin-auth.service';
import { ADMIN_WEBMCP_PROVIDERS } from './admin-webmcp.providers';
import { AdminWebmcpService } from './admin-webmcp.service';

const DIAGNOSTICS = 'meta[name="fooddiary-webmcp"]';
const TOOL_COUNT = 4;
const TOO_LONG_FILTER_LENGTH = 201;

@Component({ template: '' })
class WebmcpHostComponent {}

function diagnostics(): unknown {
    const meta = document.querySelector<HTMLMetaElement>(DIAGNOSTICS);
    return meta === null ? null : (JSON.parse(meta.content) as unknown);
}

function registrationFixture(hasBrowserApi = true): {
    fixture: ComponentFixture<WebmcpHostComponent>;
    service: AdminWebmcpService;
    admin: ReturnType<typeof signal<boolean>>;
    authenticated: ReturnType<typeof signal<boolean>>;
    session: ReturnType<typeof signal<string>>;
    tools: Map<string, WebmcpTool>;
    browserSignals: AbortSignal[];
    navigateByUrl: ReturnType<typeof vi.fn>;
    router: { url: string };
} {
    const tools = new Map<string, WebmcpTool>();
    const browserSignals: AbortSignal[] = [];
    const modelContext: WebmcpModelContext = {
        registerTool: (tool, options): void => {
            tools.set(tool.name, tool);
            if (options?.signal !== undefined) {
                browserSignals.push(options.signal);
            }
        },
    };
    const existing = Object.getOwnPropertyDescriptor(document, 'modelContext');
    Object.defineProperty(document, 'modelContext', { configurable: true, value: hasBrowserApi ? modelContext : undefined });
    onTestFinished(() => {
        TestBed.resetTestingModule();
        document.querySelector(DIAGNOSTICS)?.remove();
        if (existing === undefined) {
            Reflect.deleteProperty(document, 'modelContext');
        } else {
            Object.defineProperty(document, 'modelContext', existing);
        }
    });

    const admin = signal(true);
    const authenticated = signal(true);
    const session = signal('current-session');
    const navigateByUrl = vi.fn<(url: string) => Promise<boolean>>().mockResolvedValue(true);
    const router = { url: '/users' };
    const navigateRouterAsync = async (url: string): Promise<boolean> => {
        const navigated = await navigateByUrl(url);
        if (navigated) {
            router.url = url;
        }
        return navigated;
    };
    TestBed.configureTestingModule({
        providers: [
            ...ADMIN_WEBMCP_PROVIDERS,
            { provide: Router, useValue: Object.assign(router, { navigateByUrl: navigateRouterAsync }) },
            { provide: AdminAuthService, useValue: { isAdmin: admin, isAuthenticated: authenticated, getSessionId: session } },
        ],
    });

    const fixture = TestBed.createComponent(WebmcpHostComponent);
    const service = TestBed.inject(AdminWebmcpService);
    return { fixture, service, admin, authenticated, session, tools, browserSignals, navigateByUrl, router };
}

async function registerAsync(context: ReturnType<typeof registrationFixture>): Promise<void> {
    context.fixture.detectChanges();
    await vi.waitFor(() => {
        expect(diagnostics()).toMatchObject({ registration: 'ready', registeredToolCount: TOOL_COUNT });
    });
}

function requireTool(tools: Map<string, WebmcpTool>, name: string): WebmcpTool {
    const tool = tools.get(`fooddiary_admin_${name}`);
    if (tool === undefined) {
        throw new Error(`The browser did not receive ${name}`);
    }
    return tool;
}

describe('Admin browser registration', () => {
    it('revokes authenticated tools and registers fresh callbacks after a new sign-in', async () => {
        const context = registrationFixture();
        await registerAsync(context);
        const read = requireTool(context.tools, 'read_screen_context');
        const first = context.browserSignals[0];

        expect(first.aborted).toBe(false);
        expect(await read.execute({})).toMatchObject({ path: '/users', screen: null });
        context.authenticated.set(false);
        context.fixture.detectChanges();
        await vi.waitFor(() => {
            expect(diagnostics()).toMatchObject({ authenticated: false, registration: 'signed-out', registeredToolCount: 0 });
        });
        expect(first.aborted).toBe(true);
        expect(await read.execute({})).toMatchObject({ ok: false });

        context.session.set('new-session');
        context.authenticated.set(true);
        await registerAsync(context);
        expect(requireTool(context.tools, 'read_screen_context')).not.toBe(read);
        expect(await requireTool(context.tools, 'read_screen_context').execute({})).toMatchObject({ role: 'Admin' });
        expect(await read.execute({})).toMatchObject({ ok: false });
    });

    it('publishes browser support guidance when the local browser has no registration API', async () => {
        const context = registrationFixture(false);
        context.fixture.detectChanges();
        await vi.waitFor(() => {
            expect(diagnostics()).toMatchObject({
                authenticated: true,
                reason: 'browser-api-unavailable',
                registration: 'not-registered',
                registeredToolCount: 0,
            });
        });
        expect(context.tools.size).toBe(0);
    });

    it('releases browser registrations when the admin application environment is destroyed', async () => {
        const context = registrationFixture();
        await registerAsync(context);
        const signals = [...context.browserSignals];
        expect(signals).toHaveLength(TOOL_COUNT);
        expect(signals.every(abortSignal => !abortSignal.aborted)).toBe(true);

        TestBed.resetTestingModule();

        expect(signals.every(abortSignal => abortSignal.aborted)).toBe(true);
        expect(diagnostics()).toMatchObject({ registration: 'disposed', registeredToolCount: 0 });
    });
});

describe('registered Admin screen tools', () => {
    it('only opens allowlisted routes and respects both existing routes and cancelled navigation', async () => {
        const context = registrationFixture();
        context.service.attach({ kind: 'users', read: () => ({}) });
        await registerAsync(context);
        const tool = requireTool(context.tools, 'open_section');

        expect(await tool.execute({ section: 'users' })).toMatchObject({ path: '/users' });
        expect(await tool.execute({ section: 'unknown' })).toMatchObject({ ok: false });
        expect(context.navigateByUrl).not.toHaveBeenCalled();

        context.navigateByUrl.mockResolvedValueOnce(false);
        expect(await tool.execute({ section: 'mail-inbox' })).toMatchObject({ ok: false });
        expect(context.router.url).toBe('/users');
        expect(await tool.execute({ section: 'mail-inbox' })).toMatchObject({ path: '/mail-inbox' });
    });

    it('keeps an open dialog in place, respects the active screen actions and forwards typed filters', async () => {
        const context = registrationFixture();
        await registerAsync(context);
        const filters = requireTool(context.tools, 'set_screen_filters');
        const openEntity = requireTool(context.tools, 'open_entity');
        const setFiltersAsync = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);
        const openEntityAsync = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);
        const detach = context.service.attach({ kind: 'users', read: () => ({}) });
        expect(await filters.execute({ filters: {} })).toMatchObject({ ok: false });
        expect(await openEntity.execute({ id: 'user-1' })).toMatchObject({ ok: false });
        detach();

        context.service.attach({
            kind: 'users',
            read: () => ({ count: 1 }),
            filterFields: ['page', 'enabled', 'search'],
            setFiltersAsync,
            openEntityAsync,
        });
        const modal = document.createElement('section');
        modal.setAttribute('role', 'dialog');
        document.body.append(modal);
        try {
            expect(await openEntity.execute({ id: 'user-1' })).toMatchObject({ ok: false });
            expect(openEntityAsync).not.toHaveBeenCalled();
        } finally {
            modal.remove();
        }

        expect(await filters.execute({ filters: { page: Number.NaN } })).toMatchObject({ ok: false });
        expect(await filters.execute({ filters: { search: 'a'.repeat(TOO_LONG_FILTER_LENGTH) } })).toMatchObject({ ok: false });
        expect(setFiltersAsync).not.toHaveBeenCalled();
        expect(await filters.execute({ filters: { page: 1, enabled: false, search: null } })).toMatchObject({ screen: { kind: 'users' } });
        expect(setFiltersAsync).toHaveBeenCalledExactlyOnceWith({ page: '1', enabled: 'false', search: null });
        expect(await openEntity.execute({ id: 'user-1' })).toMatchObject({ screen: { kind: 'users', data: { count: 1 } } });
        expect(openEntityAsync).toHaveBeenCalledExactlyOnceWith('user-1');
    });
});

describe('published Admin screen state', () => {
    it('requires current screen state and keeps an empty published filter schema empty', async () => {
        const context = registrationFixture();
        await registerAsync(context);
        const filters = requireTool(context.tools, 'set_screen_filters');
        const openSection = requireTool(context.tools, 'open_section');
        expect(await openSection.execute({ section: 'mail-inbox' })).toMatchObject({ ok: false });
        expect(context.navigateByUrl).not.toHaveBeenCalled();

        const setFiltersAsync = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);
        context.service.attach({ kind: 'users', read: () => ({ count: 0 }), setFiltersAsync });
        expect(await filters.execute({ filters: {} })).toMatchObject({ screen: { kind: 'users', filterFields: [] } });
        expect(setFiltersAsync).toHaveBeenCalledExactlyOnceWith({});
    });
});
