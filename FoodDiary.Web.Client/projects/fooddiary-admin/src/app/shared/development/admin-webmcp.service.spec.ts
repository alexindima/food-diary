import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAuthService } from '../../features/admin-auth/lib/admin-auth.service';
import { AdminWebmcpService } from './admin-webmcp.service';

describe('local admin WebMCP capabilities', () => {
    const session = 'test-session-value';
    const auth = { isAuthenticated: vi.fn(), isAdmin: vi.fn(), getSessionId: vi.fn() };
    const navigateByUrl = vi.fn().mockResolvedValue(true);
    let service: AdminWebmcpService;
    const callAsync = async (name: string, input: unknown): Promise<unknown> => {
        const tool = service['tools'](session).find(item => item.name === `fooddiary_admin_${name}`);
        if (tool === undefined) {
            throw new Error('Missing tool.');
        }
        return tool.execute(input);
    };
    beforeEach(() => {
        vi.clearAllMocks();
        auth.isAuthenticated.mockReturnValue(true);
        auth.isAdmin.mockReturnValue(true);
        auth.getSessionId.mockReturnValue(session);
        TestBed.configureTestingModule({
            providers: [
                { provide: AdminAuthService, useValue: auth },
                { provide: Router, useValue: { url: '/users?code=private#private', navigateByUrl } },
            ],
        });
        service = TestBed.inject(AdminWebmcpService);
    });

    it('returns only explicitly published state and excludes URL secrets and session material', async () => {
        service.attach({ kind: 'users', read: () => ({ items: [{ id: 'owned-user' }] }) });
        const result = await callAsync('read_screen_context', {});
        expect(result).toMatchObject({ path: '/users', role: 'Admin', screen: { kind: 'users', data: { items: [{ id: 'owned-user' }] } } });
        expect(JSON.stringify(result)).not.toContain('private');
        expect(JSON.stringify(result)).not.toContain(session);
    });

    it('refuses stale sessions and non-admin access', async () => {
        auth.getSessionId.mockReturnValue('changed-session');
        expect(await callAsync('open_section', { section: 'users' })).toMatchObject({ ok: false });
        auth.getSessionId.mockReturnValue(session);
        auth.isAdmin.mockReturnValue(false);
        expect(await callAsync('read_screen_context', {})).toMatchObject({ ok: false });
        expect(navigateByUrl).not.toHaveBeenCalled();
    });

    it('checks dirty state and rejects arbitrary filters before navigation', async () => {
        const setFiltersAsync = vi.fn();
        let dirty = true;
        service.attach({ kind: 'users', read: () => ({}), isDirty: () => dirty, filterFields: ['page', 'search'], setFiltersAsync });
        expect(await callAsync('open_section', { section: 'mail-inbox' })).toMatchObject({ ok: false });
        dirty = false;
        expect(await callAsync('set_screen_filters', { filters: { token: 'private' } })).toMatchObject({ ok: false });
        expect(setFiltersAsync).not.toHaveBeenCalled();
        await callAsync('set_screen_filters', { filters: { search: 'owned' } });
        expect(setFiltersAsync).toHaveBeenCalledWith({ page: null, search: 'owned' });
    });

    it('preserves the underlying page when a dialog detaches', async () => {
        service.attach({ kind: 'page', read: () => ({}) });
        const dispose = service.attach({ kind: 'dialog', read: () => ({}) });
        dispose();
        expect(await callAsync('read_screen_context', {})).toMatchObject({ screen: { kind: 'page' } });
    });
});
