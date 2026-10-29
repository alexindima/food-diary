import { OverlayContainer } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdTourHostComponent, FdTourService } from 'fd-tour';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { HeaderActionsOverflowComponent } from './header-actions-overflow';

@Component({
    imports: [HeaderActionsOverflowComponent, FdTourHostComponent],
    template: `
        <fd-header-actions-overflow>
            <button data-tour-id="tour-appearance" (click)="clicks = clicks + 1">Appearance</button>
            <button data-tour-id="tour-help" (click)="clicks = clicks + 1">Help</button>
        </fd-header-actions-overflow>
        <div data-tour-id="tour-content">Content</div>
        <fd-tour-host />
    `,
})
class TourHeaderHost {
    public clicks = 0;
}

const HEADER_TOUR_STEP_COUNT = 3;

let fixture: ComponentFixture<TourHeaderHost>;
let service: FdTourService;
let overlay: HTMLElement;

beforeEach(async () => {
    await TestBed.configureTestingModule({
        imports: [TourHeaderHost],
        providers: [provideTranslateTesting(), provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(TourHeaderHost);
    service = TestBed.inject(FdTourService);
    overlay = TestBed.inject(OverlayContainer).getContainerElement();
    await settleAsync();
});

afterEach(() => {
    service.close();
    fixture.destroy();
});

async function settleAsync(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
}

function setMobile(mobile: boolean): void {
    const host = fixture.nativeElement as HTMLElement;
    const originals = host.querySelector<HTMLElement>('.fd-header-actions-overflow__actions');
    const trigger = host.querySelector<HTMLElement>('.fd-header-actions-overflow__trigger');
    if (originals === null || trigger === null) {
        throw new Error('Expected the responsive header');
    }
    // jsdom does not apply media queries.
    originals.style.visibility = mobile ? 'hidden' : 'visible';
    trigger.style.display = mobile ? 'inline-flex' : 'none';
}

function start(): void {
    service.start(
        TestBed.inject(LocalizedTourDefinitionService).build({
            id: 'responsive-header-regression',
            translationRoot: 'TEST',
            steps: ['appearance', 'help', 'content'].map(id => ({
                id,
                target: `tour-${id}`,
                titleKey: id,
                descriptionKey: id,
                placement: 'bottom',
            })),
        }),
        { force: true },
    );
}

describe('Responsive header tour', () => {
    it('reveals the specific hidden action without executing it', async () => {
        setMobile(true);
        start();
        await settleAsync();
        expect(overlay.querySelector('[data-tour-id="tour-appearance"]')).not.toBeNull();
        expect(service.activeStep()?.id).toBe('appearance');
        expect(service.snapshot()?.stepCount).toBe(HEADER_TOUR_STEP_COUNT);
        expect(fixture.componentInstance.clicks).toBe(0);
        service.next();
        await settleAsync();
        expect(overlay.querySelector('[data-tour-id="tour-help"]')).not.toBeNull();
        service.previous();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('appearance');
        service.next();
        service.next();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('content');
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });

    it('keeps desktop actions directly visible without opening a menu', async () => {
        setMobile(false);
        start();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('appearance');
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });

    it('closes a menu revealed by the tour on Escape', async () => {
        setMobile(true);
        start();
        await settleAsync();
        expect(overlay.querySelector('[role="menu"]')).not.toBeNull();
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', cancelable: true }));
        await settleAsync();
        expect(service.snapshot()).toBeNull();
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });

    it('re-resolves the same step when switching between mobile and desktop', async () => {
        setMobile(false);
        start();
        await settleAsync();
        const original = service.target();
        setMobile(true);
        service.refreshTarget();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('appearance');
        expect(overlay.contains(service.target())).toBe(true);
        setMobile(false);
        service.refreshTarget();
        await settleAsync();
        expect(service.target()).toBe(original);
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });

    it.each(['close', 'skip', 'complete'] as const)('releases a revealed menu on %s', async action => {
        setMobile(true);
        start();
        await settleAsync();
        expect(overlay.querySelector('[role="menu"]')).not.toBeNull();
        service[action]();
        await settleAsync();
        expect(service.snapshot()).toBeNull();
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });

    it('cancels a pending reveal when a different step is selected immediately', async () => {
        setMobile(true);
        start();
        service.next();
        service.next();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('content');
        expect(service.target()?.getAttribute('data-tour-id')).toBe('tour-content');
        expect(overlay.querySelector('[role="menu"]')).toBeNull();
    });
});

describe('Tour launch from the overflow menu', () => {
    it('keeps a revealed first step open after selecting the tour action', async () => {
        setMobile(true);
        const root = fixture.nativeElement as HTMLElement;
        root.querySelector('[data-tour-id="tour-help"]')?.addEventListener('click', start);
        root.querySelector<HTMLButtonElement>('.fd-header-actions-overflow__trigger button')?.click();
        await settleAsync();
        overlay.querySelector<HTMLButtonElement>('[data-tour-id="tour-help"] button')?.click();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('appearance');
        expect(overlay.contains(service.target())).toBe(true);
    });
});
