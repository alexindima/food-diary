import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { FdUiSidebarComponent } from './fd-ui-sidebar';
import type {
    FdUiSidebarActionRequest,
    FdUiSidebarRouteRequest,
    FdUiSidebarSection,
    FdUiSidebarSectionRequest,
} from './fd-ui-sidebar.models';
import { FdUiSidebarItemComponent } from './fd-ui-sidebar-item';
import { FdUiSidebarSectionComponent } from './fd-ui-sidebar-section';

@Component({
    selector: 'fd-ui-sidebar-test-host',
    imports: [FdUiSidebarComponent],
    template: `
        <fd-ui-sidebar
            brandTitle="FoodDiary"
            brandSubtitle="Admin"
            logoText="FD"
            notificationAriaLabel="Notifications"
            notificationHint="Open notifications"
            [notificationBadge]="2"
            [pendingRoute]="pendingRoute"
            [sections]="sections"
            [bottomSections]="bottomSections"
            [collapsed]="collapsed()"
            collapseAriaLabel="Toggle navigation"
            (notificationClick)="notificationClicks = notificationClicks + 1"
            (routeSelected)="onRouteSelected($event)"
            (actionSelected)="onActionSelected($event)"
            (sectionToggled)="onSectionToggled($event)"
            (collapsedToggle)="collapseToggleCount = collapseToggleCount + 1"
        >
            <div fdUiSidebarFooter class="test-sidebar-footer">Footer slot</div>
        </fd-ui-sidebar>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class FdUiSidebarTestHostComponent {
    public readonly collapsed = signal(false);
    public collapseToggleCount = 0;
    public pendingRoute = '/reports';
    public notificationClicks = 0;
    public selectedRouteIds: string[] = [];
    public selectedActionIds: string[] = [];
    public toggledSectionIds: string[] = [];

    public sections: FdUiSidebarSection[] = [
        {
            id: 'primary',
            items: [
                { id: 'dashboard', icon: 'dashboard', label: 'Dashboard', route: '/', exact: true },
                { id: 'admin', icon: 'admin_panel_settings', label: 'Admin panel', action: 'openAdminPanel', tone: 'danger' },
            ],
        },
        {
            id: 'food',
            title: 'Food',
            collapsible: true,
            expanded: true,
            secondary: true,
            items: [{ id: 'meals', icon: 'restaurant_menu', label: 'Meals', route: '/meals' }],
        },
    ];

    public bottomSections: FdUiSidebarSection[] = [
        {
            id: 'bottom',
            secondary: true,
            items: [
                { id: 'reports', icon: 'bar_chart', label: 'Reports', route: '/reports' },
                { id: 'goals', icon: 'flag', label: 'Goals', route: '/goals' },
            ],
        },
    ];

    public onRouteSelected(event: FdUiSidebarRouteRequest): void {
        this.selectedRouteIds.push(event.item.id);
    }

    public onActionSelected(event: FdUiSidebarActionRequest): void {
        this.selectedActionIds.push(event.item.id);
    }

    public onSectionToggled(event: FdUiSidebarSectionRequest): void {
        this.toggledSectionIds.push(event.section.id);
    }
}

const COLLAPSED_ITEM_COUNT = 5;

function setup(): ComponentFixture<FdUiSidebarTestHostComponent> {
    TestBed.configureTestingModule({
        imports: [FdUiSidebarTestHostComponent],
        providers: [provideRouter([])],
    });

    const fixture = TestBed.createComponent(FdUiSidebarTestHostComponent);
    fixture.detectChanges();

    return fixture;
}

function host(fixture: ComponentFixture<FdUiSidebarTestHostComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

describe('FdUiSidebarComponent', () => {
    it('should render brand text and notification state', () => {
        const fixture = setup();
        const nativeEl = host(fixture);

        expect(nativeEl.querySelector('.fd-ui-sidebar__brand-title')?.textContent).toContain('FoodDiary');
        expect(nativeEl.querySelector('.fd-ui-sidebar__brand-subtitle')?.textContent).toContain('Admin');
        expect(nativeEl.querySelector('.fd-ui-sidebar__brand-notifications-dot')).toBeTruthy();
    });

    it('should render bottom sections with the bottom placement class', () => {
        const fixture = setup();
        const nativeEl = host(fixture);
        const bottomSlot = nativeEl.querySelector<HTMLElement>('.fd-ui-sidebar__section-slot--bottom');

        expect(bottomSlot).toBeTruthy();
        expect(bottomSlot?.textContent).toContain('Reports');
        expect(bottomSlot?.textContent).toContain('Goals');
    });

    it('should render the footer after the spacer container', () => {
        const fixture = setup();
        const nativeEl = host(fixture);
        const scrollContainer = nativeEl.querySelector<HTMLElement>('.fd-ui-sidebar__scroll');
        const children = Array.from(scrollContainer?.children ?? []);
        const spacerIndex = children.findIndex(child => child.classList.contains('fd-ui-sidebar__spacer'));
        const footerIndex = children.findIndex(child => child.classList.contains('fd-ui-sidebar__footer'));

        expect(spacerIndex).toBeGreaterThan(-1);
        expect(footerIndex).toBeGreaterThan(spacerIndex);
        expect(nativeEl.querySelector('.test-sidebar-footer')?.textContent).toContain('Footer slot');
    });

    it('should emit notification click', () => {
        const fixture = setup();
        const button = host(fixture).querySelector<HTMLButtonElement>('.fd-ui-sidebar__brand-notifications button');

        button?.click();

        expect(fixture.componentInstance['notificationClicks']).toBe(1);
    });

    it('should render every item as an accessible icon control when collapsed', () => {
        const fixture = setup();
        fixture.componentInstance.collapsed.set(true);
        fixture.componentInstance.sections[1].expanded = false;
        fixture.detectChanges();
        const nativeEl = host(fixture);
        const controls = nativeEl.querySelectorAll<HTMLElement>('.fd-ui-sidebar__link--collapsed');

        expect(nativeEl.querySelector('.fd-ui-sidebar--collapsed')).toBeTruthy();
        expect(controls).toHaveLength(COLLAPSED_ITEM_COUNT);
        expect(Array.from(controls).map(control => control.getAttribute('aria-label'))).toEqual([
            'Dashboard',
            'Admin panel',
            'Meals',
            'Reports',
            'Goals',
        ]);
        expect(nativeEl.querySelector('.fd-ui-sidebar__section-header')).toBeNull();
    });

    it('should emit collapse toggle', () => {
        const fixture = setup();
        const button = host(fixture).querySelector<HTMLButtonElement>('.fd-ui-sidebar__collapse-toggle button');

        button?.click();

        expect(fixture.componentInstance.collapseToggleCount).toBe(1);
    });

    it('should emit route, action, and section toggle events', () => {
        const fixture = setup();
        const reportsItem = fixture.componentInstance['bottomSections'][0].items[0];
        const adminItem = fixture.componentInstance['sections'][0].items[1];
        const foodSection = fixture.componentInstance['sections'][1];
        const sidebarItems = fixture.debugElement.queryAll(By.directive(FdUiSidebarItemComponent));
        const sidebarSections = fixture.debugElement.queryAll(By.directive(FdUiSidebarSectionComponent));

        sidebarItems[0].triggerEventHandler('routeSelected', reportsItem);
        sidebarItems[1].triggerEventHandler('actionSelected', adminItem);
        sidebarSections[1].triggerEventHandler('sectionToggled', { section: foodSection });

        expect(fixture.componentInstance['selectedRouteIds']).toContain('reports');
        expect(fixture.componentInstance['selectedActionIds']).toContain('admin');
        expect(fixture.componentInstance['toggledSectionIds']).toContain('food');
    });
});
