import type { Route, Routes } from '@angular/router';

type ActivationGuard = NonNullable<Route['canActivate']>[number];

export function requireSessionForGuardedRoutes(routes: Routes, authenticatingGuards: readonly ActivationGuard[]): Routes {
    return routes.map(route => {
        const requiresSession = route.canActivate?.some(guard => authenticatingGuards.includes(guard)) === true;
        return requiresSession ? { ...route, data: { ...route.data, requiresSession: true } } : route;
    });
}
