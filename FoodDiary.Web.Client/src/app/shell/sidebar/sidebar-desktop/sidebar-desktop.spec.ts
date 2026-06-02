import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { type LangChangeEvent, TranslateService, type TranslationChangeEvent } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { User } from '../../../shared/models/user.data';
import type { SidebarActionItem, SidebarRouteItem } from '../sidebar-lib/sidebar.models';
import { SidebarDesktopComponent } from './sidebar-desktop';

const SECTION_COUNT = 3;
const UNREAD_NOTIFICATION_COUNT = 3;

const primaryRouteItems: SidebarRouteItem[] = [
    { id: 'dashboard', icon: 'dashboard', labelKey: 'SIDEBAR.DASHBOARD', route: '/dashboard', exact: true },
];

const foodTrackingItems: SidebarRouteItem[] = [{ id: 'meals', icon: 'restaurant', labelKey: 'SIDEBAR.FOOD_DIARY', route: '/meals' }];

const bodyTrackingItems: SidebarRouteItem[] = [
    { id: 'weight', icon: 'monitor_weight', labelKey: 'SIDEBAR.WEIGHT_HISTORY', route: '/weight-history' },
];

const desktopBottomItems: SidebarRouteItem[] = [{ id: 'settings', icon: 'settings', labelKey: 'SIDEBAR.SETTINGS', route: '/settings' }];

const primaryActionItems: SidebarActionItem[] = [
    { id: 'admin', icon: 'admin_panel_settings', labelKey: 'SIDEBAR.ADMIN', action: 'openAdminPanel' },
    { id: 'logout', icon: 'logout', labelKey: 'HEADER.LOGOUT', action: 'logout', variant: 'danger', badge: 2 },
];

const user: User = {
    id: 'user-1',
    email: 'user@example.com',
    username: 'Alex',
    hasPassword: true,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 8,
    fastingCheckInFollowUpReminderHours: 2,
    isActive: true,
    isEmailConfirmed: true,
};

type SidebarDesktopHarness = {
    onSidebarActionSelected: (item: { id: string }) => void;
    onSidebarRouteSelected: (item: { id: string }) => void;
    onSidebarSectionToggle: (sectionId: string) => void;
    sidebarBottomSections: () => Array<{ id: string; items: Array<{ id: string; label: string; route: string }> }>;
    sidebarSections: () => Array<{
        collapsible?: boolean;
        expanded?: boolean;
        id: string;
        items: Array<{ action?: string; badge?: number; id: string; label: string; route?: string; tone?: string }>;
        title?: string;
    }>;
};

describe('SidebarDesktopComponent sections', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('maps route and action items into translated sidebar sections', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarDesktopHarness;

        const sections = harness.sidebarSections();
        const primaryItems = sections[0]?.items ?? [];

        expect(sections).toHaveLength(SECTION_COUNT);
        expect(sections[1]).toMatchObject({
            collapsible: true,
            expanded: true,
            id: 'food',
            title: 'translated:SIDEBAR.FOOD_TRACKING',
        });
        expect(sections[2]).toMatchObject({
            collapsible: true,
            expanded: false,
            id: 'body',
            title: 'translated:SIDEBAR.BODY_TRACKING',
        });
        expect(primaryItems).toContainEqual({
            exact: true,
            icon: 'dashboard',
            id: 'dashboard',
            label: 'translated:SIDEBAR.DASHBOARD',
            route: '/dashboard',
        });
        expect(primaryItems).toContainEqual({
            action: 'openAdminPanel',
            badge: undefined,
            icon: 'admin_panel_settings',
            id: 'admin',
            label: 'translated:SIDEBAR.ADMIN',
            tone: 'danger',
        });
        expect(primaryItems).toContainEqual({
            action: 'logout',
            badge: 2,
            icon: 'logout',
            id: 'logout',
            label: 'translated:HEADER.LOGOUT',
            tone: 'danger',
        });
    });

    it('maps bottom route items into secondary bottom section', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarDesktopHarness;

        expect(harness.sidebarBottomSections()).toEqual([
            {
                id: 'bottom',
                items: [
                    {
                        exact: undefined,
                        icon: 'settings',
                        id: 'settings',
                        label: 'translated:SIDEBAR.SETTINGS',
                        route: '/settings',
                    },
                ],
                secondary: true,
            },
        ]);
    });
});

describe('SidebarDesktopComponent events', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('emits section toggle events for known collapsible sections', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarDesktopHarness;
        const foodToggle = vi.fn();
        const bodyToggle = vi.fn();
        component.foodTrackingToggle.subscribe(foodToggle);
        component.bodyTrackingToggle.subscribe(bodyToggle);

        harness.onSidebarSectionToggle('food');
        harness.onSidebarSectionToggle('body');
        harness.onSidebarSectionToggle('unknown');

        expect(foodToggle).toHaveBeenCalledOnce();
        expect(bodyToggle).toHaveBeenCalledOnce();
    });

    it('emits matching route items from any desktop section', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarDesktopHarness;
        const routeSelected = vi.fn();
        component.routeSelected.subscribe(routeSelected);

        harness.onSidebarRouteSelected({ id: 'weight' });
        harness.onSidebarRouteSelected({ id: 'settings' });
        harness.onSidebarRouteSelected({ id: 'missing' });

        expect(routeSelected).toHaveBeenCalledTimes(2);
        expect(routeSelected).toHaveBeenNthCalledWith(1, bodyTrackingItems[0]);
        expect(routeSelected).toHaveBeenNthCalledWith(2, desktopBottomItems[0]);
    });

    it('emits matching primary actions and ignores unknown actions', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarDesktopHarness;
        const primaryAction = vi.fn();
        component.primaryAction.subscribe(primaryAction);

        harness.onSidebarActionSelected({ id: 'admin' });
        harness.onSidebarActionSelected({ id: 'missing' });

        expect(primaryAction).toHaveBeenCalledOnce();
        expect(primaryAction).toHaveBeenCalledWith('openAdminPanel');
    });
});

function createComponent(): {
    component: SidebarDesktopComponent;
    fixture: ComponentFixture<SidebarDesktopComponent>;
} {
    const onLangChange = new Subject<LangChangeEvent>();
    const onTranslationChange = new Subject<TranslationChangeEvent>();
    const translateService = {
        instant: (key: string): string => `translated:${key}`,
        onLangChange,
        onTranslationChange,
    } satisfies Pick<TranslateService, 'instant' | 'onLangChange' | 'onTranslationChange'>;

    TestBed.configureTestingModule({
        imports: [SidebarDesktopComponent],
        providers: [{ provide: TranslateService, useValue: translateService }],
    });
    TestBed.overrideComponent(SidebarDesktopComponent, {
        set: {
            imports: [],
            template: '',
        },
    });

    const fixture = TestBed.createComponent(SidebarDesktopComponent);
    fixture.componentRef.setInput('brandStatusKey', 'SIDEBAR.STATUS');
    fixture.componentRef.setInput('unreadNotificationCount', UNREAD_NOTIFICATION_COUNT);
    fixture.componentRef.setInput('primaryRouteItems', primaryRouteItems);
    fixture.componentRef.setInput('primaryActionItems', primaryActionItems);
    fixture.componentRef.setInput('pendingRoute', null);
    fixture.componentRef.setInput('foodTrackingItems', foodTrackingItems);
    fixture.componentRef.setInput('bodyTrackingItems', bodyTrackingItems);
    fixture.componentRef.setInput('desktopBottomItems', desktopBottomItems);
    fixture.componentRef.setInput('isFoodTrackingOpen', true);
    fixture.componentRef.setInput('isBodyTrackingOpen', false);
    fixture.componentRef.setInput('currentUser', user);
    fixture.componentRef.setInput('userPlanLabelKey', 'SIDEBAR.PREMIUM');
    fixture.componentRef.setInput('isUserMenuOpen', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}
