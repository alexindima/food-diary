import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdTourService } from './fd-tour.service';
import { FdTourHostComponent } from './fd-tour-host';
import { FdTourTargetRegistry } from './fd-tour-target-registry';

describe('Tour target preparation lifecycle', () => {
    let service: FdTourService;
    let registry: FdTourTargetRegistry;
    let container: HTMLElement;
    let fixture: ReturnType<typeof TestBed.createComponent<FdTourHostComponent>>;

    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
        service = TestBed.inject(FdTourService);
        registry = TestBed.inject(FdTourTargetRegistry);
        fixture = TestBed.createComponent(FdTourHostComponent);
        container = document.createElement('div');
        container.innerHTML = '<button id="ready-target">Ready</button>';
        document.body.append(container);
    });

    afterEach(() => {
        service.close();
        fixture.destroy();
        container.remove();
    });

    async function settleAsync(): Promise<void> {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    }

    function start(): void {
        service.start(
            {
                id: 'preparation',
                version: 1,
                steps: [
                    { id: 'deferred', target: '#deferred-target', title: 'Deferred' },
                    { id: 'ready', target: '#ready-target', title: 'Ready' },
                ],
            },
            { force: true },
        );
    }

    it('cleans up a failed reveal and advances to the next available step', async () => {
        const cleanup = vi.fn();
        registry.register({ canReveal: selector => selector === '#deferred-target', reveal: () => cleanup });
        start();
        expect(service.preparing()).toBe(true);
        await settleAsync();
        expect(service.preparing()).toBe(false);
        expect(service.activeStep()?.id).toBe('ready');
        expect(cleanup).toHaveBeenCalledOnce();
    });

    it('closes and cleans up when the active container unregisters', async () => {
        const cleanup = vi.fn();
        const unregister = registry.register({
            canReveal: selector => selector === '#deferred-target',
            reveal: () => {
                container.insertAdjacentHTML('beforeend', '<button id="deferred-target">Revealed</button>');
                return cleanup;
            },
        });
        start();
        await settleAsync();
        expect(service.activeStep()?.id).toBe('deferred');
        unregister();
        await settleAsync();
        expect(service.snapshot()).toBeNull();
        expect(cleanup).toHaveBeenCalledOnce();
    });

    it('cancels a pending reveal when replaced by another tour', async () => {
        const cleanup = vi.fn();
        registry.register({ canReveal: selector => selector === '#deferred-target', reveal: () => cleanup });
        start();
        service.start({ id: 'replacement', version: 1, steps: [{ id: 'ready', target: '#ready-target', title: 'Ready' }] });
        await settleAsync();
        expect(service.activeTour()?.id).toBe('replacement');
        expect(service.target()?.id).toBe('ready-target');
        expect(cleanup).toHaveBeenCalledOnce();
    });
});

describe('Responsive target replacement', () => {
    it('retains the step while a responsive view replaces its target', async () => {
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const service = TestBed.inject(FdTourService);
        const container = document.createElement('div');
        container.innerHTML = '<button id="responsive-action">Mobile</button>';
        document.body.append(container);
        try {
            service.start({ id: 'replacement-view', version: 1, steps: [{ id: 'action', target: '#responsive-action', title: 'Action' }] });
            fixture.detectChanges();
            container.replaceChildren();
            service.refreshTarget();
            fixture.detectChanges();
            await fixture.whenStable();
            expect(service.activeStep()?.id).toBe('action');
            container.innerHTML = '<button id="responsive-action">Desktop</button>';
            await new Promise(resolve => setTimeout(resolve, 0));
            fixture.detectChanges();
            await fixture.whenStable();
            expect(service.target()?.textContent).toBe('Desktop');
        } finally {
            service.close();
            fixture.destroy();
            container.remove();
        }
    });
});
