import type { CanActivateFn, Routes } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { requireSessionForGuardedRoutes } from './session-route-policy';

describe('session route requirements', () => {
    const signedIn: CanActivateFn = () => true;
    const dietologist: CanActivateFn = () => true;
    const locale: CanActivateFn = () => true;

    it('marks either authenticating guard while preserving all existing route data and guards', () => {
        const source: Routes = [
            { path: 'workspace', canActivate: [locale, signedIn], data: { title: 'Workspace' } },
            { path: 'dietologist', canActivate: [dietologist] },
        ];
        const routes = requireSessionForGuardedRoutes(source, [signedIn, dietologist]);
        expect(routes[0].data).toEqual({ title: 'Workspace', requiresSession: true });
        expect(routes[1].data?.['requiresSession']).toBe(true);
        expect(routes[0].canActivate).toBe(source[0].canActivate);
        expect(source[0].data).toEqual({ title: 'Workspace' });
    });

    it('preserves guest routes and explicitly authenticated pages with public shell chrome', () => {
        const source: Routes = [
            { path: 'explore' },
            { path: 'ru', canActivate: [locale], data: { shell: 'public' } },
            { path: 'change-password-required', data: { shell: 'public', requiresSession: true } },
        ];
        const routes = requireSessionForGuardedRoutes(source, [signedIn, dietologist]);
        expect(routes[0].data?.['requiresSession']).not.toBe(true);
        expect(routes[1].data).toEqual({ shell: 'public' });
        expect(routes[2].data).toEqual({ shell: 'public', requiresSession: true });
    });
});
