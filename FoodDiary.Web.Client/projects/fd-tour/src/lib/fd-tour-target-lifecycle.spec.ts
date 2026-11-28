import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdTourService } from './fd-tour.service';
import { FdTourHostComponent } from './fd-tour-host';
import { FD_TOUR_STORAGE } from './fd-tour-storage';
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
        fixture.componentRef.setInput('pageKey', '/editor');
        TestBed.inject(Location).go('/editor');
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

describe('Page-owned tours', () => {
    const definition = {
        id: 'page-owned-editor-tour',
        version: 1,
        steps: [{ id: 'editor', target: '#page-owned-target', title: 'Editor help' }],
    };
    let fixture: ReturnType<typeof TestBed.createComponent<FdTourHostComponent>>;
    let service: FdTourService;
    let location: Location;
    let target: HTMLButtonElement;

    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
        fixture = TestBed.createComponent(FdTourHostComponent);
        service = TestBed.inject(FdTourService);
        location = TestBed.inject(Location);
        location.go('/editor', 'filter=liked');
        target = document.createElement('button');
        target.id = 'page-owned-target';
        document.body.append(target);
    });

    afterEach(() => {
        service.close();
        fixture.destroy();
        target.remove();
    });

    it('starts from an idle host and dismisses on page change without recording completion', () => {
        const host = fixture.nativeElement as HTMLElement;
        fixture.componentRef.setInput('pageKey', '/editor');
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();

        expect(service.start(definition)).toBe(true);
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')?.getAttribute('aria-label')).toBe('Editor help');

        location.go('/overview');
        fixture.componentRef.setInput('pageKey', '/overview');
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();
        expect(service.snapshot()).toBeNull();
        expect(TestBed.inject(FD_TOUR_STORAGE).isCompleted(definition.id, definition.version)).toBe(false);
    });

    it('discards a tour started on another page before its host mounts even if the target selector still matches', () => {
        expect(service.start(definition)).toBe(true);
        expect(service.target()).toBe(target);
        location.go('/overview');
        const previousTarget = target;
        target = document.createElement('button');
        target.id = previousTarget.id;
        previousTarget.replaceWith(target);

        fixture.componentRef.setInput('pageKey', '/overview');
        fixture.detectChanges();

        expect((fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]')).toBeNull();
        expect(service.snapshot()).toBeNull();
        expect(service.target()).toBeNull();
    });
});

describe('Responsive target replacement', () => {
    it('binds a replacement target when the page-owned host first mounts', async () => {
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
        const fixture = TestBed.createComponent(FdTourHostComponent);
        fixture.componentRef.setInput('pageKey', '/editor');
        TestBed.inject(Location).go('/editor');
        const service = TestBed.inject(FdTourService);
        const original = document.createElement('button');
        original.id = 'responsive-original';
        const replacement = document.createElement('button');
        replacement.id = 'responsive-fallback';
        document.body.append(original);
        try {
            const definition = {
                id: 'responsive-before-host',
                version: 1,
                steps: [{ id: 'action', target: '#responsive-original, #responsive-fallback', title: 'Action' }],
            };
            expect(service.start(definition)).toBe(true);
            expect(service.target()).toBe(original);
            original.replaceWith(replacement);
            fixture.detectChanges();
            await fixture.whenStable();

            expect(service.activeTour()).toBe(definition);
            expect(service.target()).toBe(replacement);
        } finally {
            service.close();
            fixture.destroy();
            original.remove();
            replacement.remove();
        }
    });

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
