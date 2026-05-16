import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { DASHBOARD_LAYOUT_CONFIG } from '../../../config/runtime-ui.tokens';
import { UserService } from '../../../shared/api/user.service';
import type { DashboardLayoutSettings } from '../../../shared/models/user.data';
import { DashboardLayoutService } from './dashboard-layout.service';

const MOBILE_WIDTH = 500;
const DESKTOP_WIDTH = 1200;

describe('DashboardLayoutService', () => {
    it('normalizes incoming layout and keeps summary visible first', () => {
        const { service } = setupService();

        service.initializeLayout({
            web: ['hydration', 'unknown', 'hydration'],
            mobile: [],
        });

        service.updateViewportWidth(DESKTOP_WIDTH);
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);

        service.updateViewportWidth(MOBILE_WIDTH);
        expect(service.visibleBlocks()[0]).toBe('summary');
        expect(service.visibleBlocks()).toContain('meals');
    });

    it('tracks layout changes during editing and persists changed layout on save', () => {
        const { service, userService } = setupService();

        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary'] });
        service.updateViewportWidth(DESKTOP_WIDTH);
        service.openSettings();
        service.toggleBlock('hydration');

        expect(service.hasLayoutChanges()).toBe(true);

        service.save();

        expect(service.isEditingLayout()).toBe(false);
        expect(userService.updateDashboardLayout).toHaveBeenCalledWith({
            web: ['summary'],
            mobile: ['summary'],
        });
    });

    it('does not persist unchanged layout when closing settings', () => {
        const { service, userService } = setupService();

        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary'] });
        service.openSettings();
        service.openSettings();

        expect(userService.updateDashboardLayout).not.toHaveBeenCalled();
    });

    it('restores the snapshot on discard', () => {
        const { service, userService } = setupService();

        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary'] });
        service.updateViewportWidth(DESKTOP_WIDTH);
        service.openSettings();
        service.toggleBlock('hydration');
        service.discard();

        expect(service.isEditingLayout()).toBe(false);
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
        expect(userService.updateDashboardLayout).not.toHaveBeenCalled();
    });
});

function setupService(): {
    service: DashboardLayoutService;
    userService: { updateDashboardLayout: ReturnType<typeof vi.fn> };
} {
    const userService = {
        updateDashboardLayout: vi.fn((layout: DashboardLayoutSettings) => of({ dashboardLayout: layout })),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [
            DashboardLayoutService,
            { provide: UserService, useValue: userService },
            {
                provide: DASHBOARD_LAYOUT_CONFIG,
                useValue: { defaultViewportWidth: DESKTOP_WIDTH, mobileBreakpointPx: 768 },
            },
        ],
    });

    return {
        service: TestBed.inject(DashboardLayoutService),
        userService,
    };
}
