import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { StatisticsBodyComponent } from './statistics-body.component';

type StatisticsBodyTestContext = {
    component: StatisticsBodyComponent;
    fixture: ComponentFixture<StatisticsBodyComponent>;
};

async function setupStatisticsBodyAsync(
    overrides: Partial<{ isLoading: boolean; hasLoadError: boolean; hasBodyData: boolean }> = {},
): Promise<StatisticsBodyTestContext> {
    await TestBed.configureTestingModule({
        imports: [StatisticsBodyComponent, TranslateModule.forRoot()],
    }).compileComponents();

    const fixture = TestBed.createComponent(StatisticsBodyComponent);
    const component = fixture.componentInstance;
    fixture.componentRef.setInput('tabs', [{ value: 'weight', label: 'Weight' }]);
    fixture.componentRef.setInput('selectedTab', 'weight');
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('hasLoadError', overrides.hasLoadError ?? false);
    fixture.componentRef.setInput('bodyChartData', null);
    fixture.componentRef.setInput('bodyChartOptions', null);
    fixture.componentRef.setInput('hasBodyData', overrides.hasBodyData ?? true);

    return { component, fixture };
}

describe('StatisticsBodyComponent state', () => {
    it('prioritizes loading state', async () => {
        const { component, fixture } = await setupStatisticsBodyAsync({ isLoading: true, hasLoadError: true, hasBodyData: false });
        fixture.detectChanges();

        expect(component.sectionState()).toBe('loading');
    });

    it('uses error state when loading is finished and load failed', async () => {
        const { component, fixture } = await setupStatisticsBodyAsync({ hasLoadError: true });
        fixture.detectChanges();

        expect(component.sectionState()).toBe('error');
    });

    it('uses empty state when there is no body data', async () => {
        const { component, fixture } = await setupStatisticsBodyAsync({ hasBodyData: false });
        fixture.detectChanges();

        expect(component.sectionState()).toBe('empty');
    });
});

describe('StatisticsBodyComponent events', () => {
    it('emits selected tab changes and retry events', async () => {
        const { component, fixture } = await setupStatisticsBodyAsync();
        const tabSpy = vi.fn();
        const retrySpy = vi.fn();
        component.selectedTabChange.subscribe(tabSpy);
        component.retry.subscribe(retrySpy);
        fixture.detectChanges();

        component.onTabChange('waist');
        component.onRetry();

        expect(tabSpy).toHaveBeenCalledWith('waist');
        expect(retrySpy).toHaveBeenCalledOnce();
    });
});
