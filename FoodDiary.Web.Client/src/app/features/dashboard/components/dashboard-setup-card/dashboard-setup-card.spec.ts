import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { DashboardSetupFacade } from '../../lib/dashboard-setup.facade';
import { DashboardSetupCardComponent } from './dashboard-setup-card';

const CARD_BUTTON_COUNT = 3;
type SetupFacadeStub = {
    isSaving: ReturnType<typeof signal<boolean>>;
    saveFailed: ReturnType<typeof signal<boolean>>;
    dismissAsync: ReturnType<typeof vi.fn>;
};

describe('DashboardSetupCardComponent', () => {
    it('offers profile or weight setup and a separate goals action', async () => {
        const { fixture, host } = await setupAsync();
        const primaryAction = vi.fn();
        const goalsAction = vi.fn();
        fixture.componentInstance.primaryAction.subscribe(primaryAction);
        fixture.componentInstance.goalsAction.subscribe(goalsAction);
        const buttons = (): HTMLButtonElement[] => Array.from(host.querySelectorAll('button'));
        expect(buttons()).toHaveLength(CARD_BUTTON_COUNT);
        expect(buttons()[1].textContent).toContain('DASHBOARD.INITIAL_SETUP.PROFILE');
        buttons()[1].click();
        buttons()[2].click();
        expect(primaryAction).toHaveBeenCalledOnce();
        expect(goalsAction).toHaveBeenCalledOnce();
        fixture.componentRef.setInput('profileComplete', true);
        fixture.detectChanges();
        expect(buttons()[1].textContent).toContain('DASHBOARD.INITIAL_SETUP.WEIGHT');
        expect(host.textContent).not.toContain('MEAL_LIST.ADD_MANUALLY_BUTTON');
    });

    it('names the close button and disables it while saving', async () => {
        const { fixture, host, facade } = await setupAsync();
        const close = host.querySelector('button[aria-label="DASHBOARD.INITIAL_SETUP.DISMISS"]') as HTMLButtonElement;
        expect(close).not.toBeNull();
        close.click();
        expect(facade.dismissAsync).toHaveBeenCalledOnce();
        facade.isSaving.set(true);
        fixture.detectChanges();
        expect(close.disabled).toBe(true);
        close.click();
        expect(facade.dismissAsync).toHaveBeenCalledOnce();
    });

    it('announces a failed save while keeping the card available for retry', async () => {
        const { fixture, host, facade } = await setupAsync();
        facade.saveFailed.set(true);
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('DASHBOARD.INITIAL_SETUP.SAVE_ERROR');
        expect(host.querySelector('button')?.disabled).toBe(false);
    });
});

async function setupAsync(): Promise<{
    fixture: ComponentFixture<DashboardSetupCardComponent>;
    host: HTMLElement;
    facade: SetupFacadeStub;
}> {
    const facade = { isSaving: signal(false), saveFailed: signal(false), dismissAsync: vi.fn() };
    await TestBed.configureTestingModule({
        imports: [DashboardSetupCardComponent],
        providers: [provideTranslateTesting()],
    })
        .overrideComponent(DashboardSetupCardComponent, {
            set: { providers: [{ provide: DashboardSetupFacade, useValue: facade }] },
        })
        .compileComponents();
    const fixture = TestBed.createComponent(DashboardSetupCardComponent);
    fixture.componentRef.setInput('profileComplete', false);
    fixture.detectChanges();
    return { fixture, host: fixture.nativeElement as HTMLElement, facade };
}
