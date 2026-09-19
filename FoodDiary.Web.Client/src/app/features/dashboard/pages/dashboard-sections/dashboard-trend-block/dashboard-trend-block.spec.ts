import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it, type MockInstance,vi } from 'vitest';

import { buildDashboardBlockState } from '../../dashboard-lib/dashboard-view-state.mapper';
import { DashboardTrendBlockComponent } from './dashboard-trend-block';

describe('Dashboard trend navigation', () => {
    it.each(['/weight-history', '/waist-history'])('opens %s in normal mode', async route => {
        const { component, navigate } = await setupAsync(false, route);
        component['activate']();
        expect(navigate).toHaveBeenCalledWith(route);
    });

    it('only toggles visibility while editing the dashboard', async () => {
        const { component, navigate } = await setupAsync(true, '/weight-history');
        const toggle = vi.fn();
        component.blockToggle.subscribe(toggle);
        component['activate']();
        expect(toggle).toHaveBeenCalledOnce();
        expect(navigate).not.toHaveBeenCalled();
    });
});

async function setupAsync(editing: boolean, route: string): Promise<{
    component: DashboardTrendBlockComponent;
    navigate: MockInstance<Router['navigateByUrl']>;
}> {
    await TestBed.configureTestingModule({ imports: [DashboardTrendBlockComponent], providers: [provideRouter([])] })
        .overrideComponent(DashboardTrendBlockComponent, { set: { template: '' } })
        .compileComponents();
    const fixture = TestBed.createComponent(DashboardTrendBlockComponent);
    fixture.componentRef.setInput('actionRoute', route);
    fixture.componentRef.setInput(
        'state',
        buildDashboardBlockState({
            blockId: 'weight',
            editing,
            isVisible: true,
            canToggle: true,
            ariaLabel: null,
            stateOptions: { alwaysInteractive: true },
        }),
    );
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    return { component: fixture.componentInstance, navigate };
}
