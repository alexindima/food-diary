import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it, type MockInstance, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { buildDashboardBlockState } from '../../dashboard-lib/dashboard-view-state.mapper';
import { DashboardTrendBlockComponent } from './dashboard-trend-block';

describe('Dashboard trend navigation', () => {
    for (const route of ['/weight-history', '/waist-history']) {
        it.each(['Enter', ' '])(`opens ${route} with the keyboard (%s)`, async key => {
            const { fixture, navigate } = await setupAsync(false, route);
            const host = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('[role="button"]');
            expect(host?.getAttribute('tabindex')).toBe('0');
            host?.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
            expect(navigate).toHaveBeenCalledExactlyOnceWith(route);
        });
    }
    it('removes a hidden block from the DOM', async () => {
        const { fixture } = await setupAsync(false, '/weight-history');
        fixture.componentRef.setInput('shouldRender', false);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="button"]')).toBeNull();
    });
    it.each(['/weight-history', '/waist-history'])('opens %s in normal mode', async route => {
        const { fixture, navigate } = await setupAsync(false, route);
        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('[role="button"]')?.click();
        expect(navigate).toHaveBeenCalledWith(route);
    });

    it('only toggles visibility while editing the dashboard', async () => {
        const { component, fixture, navigate } = await setupAsync(true, '/weight-history');
        const toggle = vi.fn();
        component.blockToggle.subscribe(toggle);
        (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('[role="button"]')?.click();
        expect(toggle).toHaveBeenCalledOnce();
        expect(navigate).not.toHaveBeenCalled();
    });
});

async function setupAsync(
    editing: boolean,
    route: string,
): Promise<{
    component: DashboardTrendBlockComponent;
    fixture: ComponentFixture<DashboardTrendBlockComponent>;
    navigate: MockInstance<Router['navigateByUrl']>;
}> {
    await TestBed.configureTestingModule({
        imports: [DashboardTrendBlockComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(DashboardTrendBlockComponent);
    fixture.componentRef.setInput('actionRoute', route);
    for (const [key, value] of Object.entries({
        shouldRender: true,
        cardClass: '',
        current: null,
        change: null,
        points: [],
        isLoading: false,
    })) {
        fixture.componentRef.setInput(key, value);
    }
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
    fixture.detectChanges();
    return { component: fixture.componentInstance, fixture, navigate };
}
