import type { ActivatedRoute, Router } from '@angular/router';

import { adminPage } from './admin-query';

export function adminPageWithinTotal(page: number, totalItems: number, pageSize: number): number {
    const total = Number.isFinite(totalItems) ? Math.max(0, totalItems) : 0;
    const size = Number.isFinite(pageSize) ? Math.max(1, Math.trunc(pageSize)) : 1;
    return Math.min(adminPage(String(page)), Math.max(1, Math.ceil(total / size)));
}

export function restoreAdminPage(
    router: Router,
    route: ActivatedRoute,
    page: number,
    pagination: { totalItems: number; pageSize: number },
): boolean {
    const target = adminPageWithinTotal(page, pagination.totalItems, pagination.pageSize);
    const current = route.snapshot.queryParamMap.get('page');
    if ((current === null && target === 1) || current === String(target)) {
        return false;
    }

    void router.navigate([], {
        relativeTo: route,
        queryParams: { page: target },
        queryParamsHandling: 'merge',
        replaceUrl: true,
    });
    return true;
}
