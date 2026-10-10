import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { UserService } from '../../../shared/api/user.service';
import type { DashboardLayoutSettings } from '../../../shared/models/user.data';
import { DashboardSetupFacade } from './dashboard-setup.facade';

describe('DashboardSetupFacade', () => {
    it('saves dismissal in account preferences while retaining desktop and mobile layouts', async () => {
        const { facade, userService } = setup();
        await facade.dismissAsync();
        expect(userService.updateDashboardLayout).toHaveBeenCalledWith({
            web: ['summary', 'hydration'],
            mobile: ['summary', 'meals'],
            setupHintDismissed: true,
        });
        expect(facade.saveFailed()).toBe(false);
        expect(facade.isSaving()).toBe(false);
    });

    it.each([of(null), throwError(() => new Error('offline'))])('allows retry when dismissal could not be saved', async failure => {
        const { facade, userService } = setup();
        userService.updateDashboardLayout.mockReturnValueOnce(failure);
        await facade.dismissAsync();
        expect(facade.saveFailed()).toBe(true);
        expect(facade.isSaving()).toBe(false);
        await facade.dismissAsync();
        expect(facade.saveFailed()).toBe(false);
        expect(userService.updateDashboardLayout).toHaveBeenCalledTimes(2);
    });

    it('ignores repeated close clicks until the pending save completes', async () => {
        const { facade, userService } = setup();
        const pending = new Subject<{ dashboardLayout: { setupHintDismissed: boolean } }>();
        userService.updateDashboardLayout.mockReturnValueOnce(pending);
        const saving = facade.dismissAsync();
        await facade.dismissAsync();
        expect(facade.isSaving()).toBe(true);
        expect(userService.updateDashboardLayout).toHaveBeenCalledOnce();
        pending.next({ dashboardLayout: { setupHintDismissed: true } });
        pending.complete();
        await saving;
        expect(facade.isSaving()).toBe(false);
        expect(facade.saveFailed()).toBe(false);
    });
});

function setup(): {
    facade: DashboardSetupFacade;
    userService: { user: WritableSignal<{ dashboardLayout: DashboardLayoutSettings }>; updateDashboardLayout: ReturnType<typeof vi.fn> };
} {
    TestBed.resetTestingModule();
    const userService = {
        user: signal<{ dashboardLayout: DashboardLayoutSettings }>({
            dashboardLayout: { web: ['summary', 'hydration'], mobile: ['summary', 'meals'] },
        }),
        updateDashboardLayout: vi.fn().mockReturnValue(of({ dashboardLayout: { setupHintDismissed: true } })),
    };
    TestBed.configureTestingModule({
        providers: [DashboardSetupFacade, { provide: UserService, useValue: userService }],
    });
    return { facade: TestBed.inject(DashboardSetupFacade), userService };
}
