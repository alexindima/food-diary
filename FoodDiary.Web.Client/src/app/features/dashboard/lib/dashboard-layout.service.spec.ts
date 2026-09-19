import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
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

describe('Dashboard layout invariants', () => {
    it('does not hide the summary or change visibility outside editing mode', () => {
        const { service, userService } = setupService();
        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary'] });
        service.toggleBlock('hydration');
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
        service.openSettings();
        service.toggleBlock('summary');
        service.save();
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
        expect(userService.updateDashboardLayout).not.toHaveBeenCalled();
    });

    it('keeps desktop and mobile visibility independent and restores both on discard', () => {
        const { service } = setupService();
        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary', 'meals'] });
        service.openSettings();
        service.toggleBlock('hydration');
        service.updateViewportWidth(MOBILE_WIDTH);
        expect(service.visibleBlocks()).toEqual(['summary', 'meals']);
        service.toggleBlock('meals');
        service.discard();
        expect(service.visibleBlocks()).toEqual(['summary', 'meals']);
        service.updateViewportWidth(DESKTOP_WIDTH);
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
    });

    it('renders hidden cards only while editing and ignores subsequent snapshot initialization', () => {
        const { service } = setupService();
        service.initializeLayout({ web: ['summary'], mobile: ['summary'] });
        expect(service.shouldRenderBlock('hydration')).toBe(false);
        service.openSettings();
        expect(service.shouldRenderBlock('hydration')).toBe(true);
        service.toggleBlock('hydration');
        service.initializeLayout({ web: ['summary', 'meals'], mobile: ['summary'] });
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
    });
});

describe('Dashboard layout persistence', () => {
    it.each([null, {}])('keeps the chosen local layout when the API returns %s', response => {
        const { service, userService } = setupService();
        userService.updateDashboardLayout.mockReturnValueOnce(of(response));
        service.initializeLayout({ web: ['summary', 'hydration'], mobile: ['summary'] });
        service.openSettings();
        service.toggleBlock('hydration');
        service.save();
        expect(service.visibleBlocks()).toEqual(['summary']);
        expect(service.hasLayoutChanges()).toBe(false);
        expect(service.isEditingLayout()).toBe(false);
    });

    it('normalizes the layout returned by the server', () => {
        const { service, userService } = setupService();
        userService.updateDashboardLayout.mockReturnValueOnce(
            of({ dashboardLayout: { web: ['weight', 'weight', 'unknown'], mobile: ['summary'] } }),
        );
        service.initializeLayout({ web: ['summary'], mobile: ['summary'] });
        expect(service.hasAsideBlocks()).toBe(false);
        service.openSettings();
        service.toggleBlock('hydration');
        service.save();
        expect(service.visibleBlocks()).toEqual(['summary', 'weight']);
        expect(service.hasAsideBlocks()).toBe(true);
    });

    it('does not apply a late save response after destruction', () => {
        const { service, userService } = setupService();
        const pending = new Subject<{ dashboardLayout: DashboardLayoutSettings }>();
        userService.updateDashboardLayout.mockReturnValueOnce(pending);
        service.initializeLayout({ web: ['summary'], mobile: ['summary'] });
        service.openSettings();
        service.toggleBlock('hydration');
        service.save();
        TestBed.resetTestingModule();
        expect(pending.observed).toBe(false);
        pending.next({ dashboardLayout: { web: ['summary', 'weight'], mobile: ['summary'] } });
        expect(service.visibleBlocks()).toEqual(['summary', 'hydration']);
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
