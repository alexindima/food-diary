import { NO_ERRORS_SCHEMA, type Type } from '@angular/core';
import { DeferBlockBehavior, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { buildDashboardBlockState } from '../dashboard-lib/dashboard-view-state.mapper';
import { DashboardAdviceBlockComponent } from './dashboard-advice-block/dashboard-advice-block';
import { DashboardCycleBlockComponent } from './dashboard-cycle-block/dashboard-cycle-block';
import { DashboardHydrationBlockComponent } from './dashboard-hydration-block/dashboard-hydration-block';
import { DashboardQuickAddComponent } from './dashboard-quick-add/dashboard-quick-add';
import { DashboardTdeeBlockComponent } from './dashboard-tdee-block/dashboard-tdee-block';
import { DashboardTrendBlockComponent } from './dashboard-trend-block/dashboard-trend-block';

const sections: Array<{ component: Type<unknown>; content: string; inputs: Record<string, unknown> }> = [
    {
        component: DashboardHydrationBlockComponent,
        content: 'fd-hydration-card',
        inputs: { cardState: { total: 800, goal: 2200 }, canAdd: true },
    },
    {
        component: DashboardCycleBlockComponent,
        content: 'fd-cycle-summary-card',
        inputs: { cardState: { cycle: null, startDate: null, predictions: null }, referenceDate: new Date('2026-10-29T00:00:00') },
    },
    {
        component: DashboardTrendBlockComponent,
        content: 'fd-weight-trend-card',
        inputs: { cardClass: '', current: 72, change: -1, points: [] },
    },
    { component: DashboardTdeeBlockComponent, content: 'fd-tdee-insight-card', inputs: { insight: null } },
    { component: DashboardAdviceBlockComponent, content: 'fd-daily-advice-card', inputs: { advice: null } },
];

describe('Dashboard section rendering', () => {
    // Keep real section templates while isolating their children. Manual defer mode
    // reproduces cards below the viewport before any scroll or idle trigger fires.
    it.each(sections)('renders $content immediately from the available snapshot', async ({ component, content, inputs }) => {
        await configureSectionAsync(component);
        const fixture = TestBed.createComponent(component);
        for (const [key, value] of Object.entries({
            shouldRender: true,
            state: buildDashboardBlockState({ blockId: 'hydration', editing: false, isVisible: true, canToggle: true, ariaLabel: null }),
            isLoading: false,
            ...inputs,
        })) {
            fixture.componentRef.setInput(key, value);
        }
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector(content), 'Dashboard content must exist before scrolling').not.toBeNull();
        expect(host.querySelector('fd-ui-loader')).toBeNull();

        fixture.componentRef.setInput('shouldRender', false);
        fixture.detectChanges();
        expect(host.querySelector(content)).toBeNull();
    });

    it('renders quick add without waiting for an idle trigger', async () => {
        await configureSectionAsync(DashboardQuickAddComponent);
        const fixture = TestBed.createComponent(DashboardQuickAddComponent);
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('fd-ai-input-action-bar')).not.toBeNull();
    });
});

async function configureSectionAsync(component: Type<unknown>): Promise<void> {
    await TestBed.configureTestingModule({
        imports: [component],
        providers: [provideRouter([]), provideTranslateTesting()],
        deferBlockBehavior: DeferBlockBehavior.Manual,
    })
        .overrideComponent(component, { set: { imports: [TranslatePipe], schemas: [NO_ERRORS_SCHEMA] } })
        .compileComponents();
}
