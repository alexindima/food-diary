import { DOCUMENT } from '@angular/common';
import { Component, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { FdTourService } from './fd-tour.service';
import type { FdTourDefinition } from './fd-tour.types';
import { FdTourHostComponent } from './fd-tour-host';
import { FD_TOUR_STORAGE, type FdTourStorage } from './fd-tour-storage';
import { FdTourTargetRegistry } from './fd-tour-target-registry';

@Component({ template: '<button data-tour-id="first">First</button>' })
class TourDrawerFixtureComponent {}

class MemoryTourStorage implements FdTourStorage {
    private readonly completedVersions = new Map<string, number>();

    public isCompleted(tourId: string, version: number): boolean {
        return this.completedVersions.get(tourId) === version;
    }

    public markCompleted(tourId: string, version: number): void {
        this.completedVersions.set(tourId, version);
    }

    public clearCompleted(tourId: string): void {
        this.completedVersions.delete(tourId);
    }
}

describe('FdTourService', () => {
    let service: FdTourService;
    let storage: MemoryTourStorage;
    let host: HTMLElement;
    let matchMediaDescriptor: PropertyDescriptor | undefined;

    beforeEach(() => {
        storage = new MemoryTourStorage();

        TestBed.configureTestingModule({
            providers: [{ provide: FD_TOUR_STORAGE, useValue: storage }],
        });

        const domDocument = TestBed.inject(DOCUMENT);
        matchMediaDescriptor = Object.getOwnPropertyDescriptor(domDocument.defaultView, 'matchMedia');
        host = domDocument.createElement('div');
        host.append(createTourButton('first', 'First'), createTourButton('second', 'Second'));
        domDocument.body.append(host);

        service = TestBed.inject(FdTourService);
    });

    afterEach(() => {
        host.remove();
        vi.restoreAllMocks();
        const viewport = TestBed.inject(DOCUMENT).defaultView;
        if (viewport !== null) {
            if (matchMediaDescriptor === undefined) {
                Reflect.deleteProperty(viewport, 'matchMedia');
            } else {
                Object.defineProperty(viewport, 'matchMedia', matchMediaDescriptor);
            }
        }
    });

    it.each([false, true])('honors reduced motion %s when scrolling to tour steps', reducedMotion => {
        verifyTourScroll(host, reducedMotion);
    });

    it('starts a tour on the first available step', () => {
        const started = service.start(createTour());

        expect(started).toBe(true);
        expect(service.snapshot()).toEqual(
            expect.objectContaining({
                stepIndex: 0,
                stepCount: 2,
                isFirstStep: true,
                isLastStep: false,
            }),
        );
        expect(service.activeStep()?.id).toBe('first');
    });

    it('keeps smooth scrolling when matchMedia is unavailable', () => {
        verifyMissingMediaSupport(host);
    });

    it('skips steps without a rendered target', () => {
        const started = service.start({
            ...createTour(),
            steps: [
                { id: 'missing', target: '[data-tour-id="missing"]', title: 'Missing' },
                { id: 'second', target: '[data-tour-id="second"]', title: 'Second' },
            ],
        });

        expect(started).toBe(true);
        expect(service.activeStep()?.id).toBe('second');
        expect(service.snapshot()?.stepCount).toBe(1);
    });

    it('marks the current tour version completed when the tour finishes', () => {
        const tour = createTour();

        service.start(tour);
        service.next();
        service.next();

        expect(service.snapshot()).toBeNull();
        expect(storage.isCompleted(tour.id, tour.version)).toBe(true);
        expect(service.start(tour)).toBe(false);
        expect(service.start(tour, { force: true })).toBe(true);
    });
});

function verifyMissingMediaSupport(host: HTMLElement): void {
    const viewport = TestBed.inject(DOCUMENT).defaultView;
    if (viewport === null) {
        throw new Error('Expected a browser viewport');
    }
    Reflect.deleteProperty(viewport, 'matchMedia');
    const scroll = vi.fn();
    Object.defineProperty(host.children[0], 'scrollIntoView', { configurable: true, value: scroll });
    expect(TestBed.inject(FdTourService).start(createTour())).toBe(true);
    expect(scroll).toHaveBeenCalledWith({ block: 'center', inline: 'center', behavior: 'smooth' });
}

function verifyTourScroll(host: HTMLElement, reducedMotion: boolean): void {
    const scroll = vi.fn();
    for (const target of host.children) {
        Object.defineProperty(target, 'scrollIntoView', { configurable: true, value: scroll });
    }
    const viewport = TestBed.inject(DOCUMENT).defaultView;
    if (viewport === null) {
        throw new Error('Expected a browser viewport');
    }
    Object.defineProperty(viewport, 'matchMedia', { configurable: true, value: vi.fn(() => ({ matches: reducedMotion })) });
    const service = TestBed.inject(FdTourService);
    service.start(createTour());
    service.next();
    expect(scroll).toHaveBeenCalledTimes(2);
    expect(scroll).toHaveBeenLastCalledWith({ block: 'center', inline: 'center', behavior: reducedMotion ? 'instant' : 'smooth' });
}

function createTour(): FdTourDefinition {
    return {
        id: 'dashboard-welcome',
        version: 1,
        steps: [
            { id: 'first', target: '[data-tour-id="first"]', title: 'First' },
            { id: 'second', target: '[data-tour-id="second"]', title: 'Second' },
        ],
    };
}

function createTourButton(tourId: string, label: string): HTMLButtonElement {
    const button = TestBed.inject(DOCUMENT).createElement('button');
    button.dataset['tourId'] = tourId;
    button.textContent = label;
    return button;
}

describe('FdTourService responsive targets', () => {
    it.each(['visibility', 'display'])('scrolls to a visible fallback when the original ancestor has hidden %s', property => {
        const host = document.createElement('div');
        host.innerHTML = '<div><button class="original">Original</button></div><button class="fallback">More actions</button>';
        document.body.append(host);
        const original = host.querySelector<HTMLElement>('.original');
        const fallback = host.querySelector<HTMLElement>('.fallback');
        if (original === null || fallback === null) {
            throw new Error('Expected responsive tour targets');
        }
        const parent = original.parentElement;
        if (parent === null) {
            throw new Error('Expected the original target container');
        }
        parent.style.setProperty(property, property === 'visibility' ? 'hidden' : 'none');
        const originalScroll = vi.fn();
        const fallbackScroll = vi.fn();
        Object.defineProperty(original, 'scrollIntoView', { configurable: true, value: originalScroll });
        Object.defineProperty(fallback, 'scrollIntoView', { configurable: true, value: fallbackScroll });
        try {
            const started = TestBed.inject(FdTourService).start(
                {
                    id: 'responsive-target',
                    version: 1,
                    steps: [{ id: 'help', target: '.original, .fallback', title: 'Help' }],
                },
                { force: true },
            );
            expect(started).toBe(true);
            expect(originalScroll).not.toHaveBeenCalled();
            expect(fallbackScroll).toHaveBeenCalledOnce();
        } finally {
            TestBed.inject(FdTourService).close();
            host.remove();
        }
    });

    it('omits a step when every matching target is hidden', () => {
        const target = document.createElement('button');
        target.id = 'hidden-tour-target';
        target.hidden = true;
        document.body.append(target);
        try {
            expect(
                TestBed.inject(FdTourService).start(
                    {
                        id: 'hidden-target',
                        version: 1,
                        steps: [{ id: 'hidden', target: '#hidden-tour-target', title: 'Hidden' }],
                    },
                    { force: true },
                ),
            ).toBe(false);
        } finally {
            TestBed.inject(FdTourService).close();
            target.remove();
        }
    });
});

describe('FdTourService navigation', () => {
    let host: HTMLElement;
    let storage: MemoryTourStorage;
    let service: FdTourService;

    beforeEach(() => {
        storage = new MemoryTourStorage();
        TestBed.configureTestingModule({ providers: [{ provide: FD_TOUR_STORAGE, useValue: storage }] });
        service = TestBed.inject(FdTourService);
        host = document.createElement('div');
        document.body.append(host);
    });

    afterEach(() => {
        host.remove();
    });

    it('leaves an idle tour empty and does not persist a completion', () => {
        const initial = createTour();

        expect(service.start({ ...initial, steps: [] })).toBe(false);
        service.next();
        service.previous();
        service.skip();
        service.complete();

        expect(service.activeStep()).toBeNull();
        expect(service.target()).toBeNull();
        expect(storage.isCompleted(initial.id, initial.version)).toBe(false);
    });

    it('skips a vanished step in either direction and stays put if no prior target survives', () => {
        const first = createTourButton('first', 'First');
        const middle = createTourButton('middle', 'Middle');
        const last = createTourButton('last', 'Last');
        host.append(first, middle, last);
        expect(service.start(createThreeStepsTour())).toBe(true);
        service.previous();
        expect(service.target()).toBe(first);

        middle.remove();
        service.next();
        expect(service.target()).toBe(last);
        expect(service.snapshot()?.isLastStep).toBe(true);
        service.previous();
        expect(service.target()).toBe(first);

        service.next();
        first.hidden = true;
        service.previous();
        expect(service.target()).toBe(last);
        expect(service.snapshot()?.isLastStep).toBe(true);
    });

    it('completes when every remaining target has disappeared', () => {
        const tour = createTour();
        const second = createTourButton('second', 'Second');
        host.append(createTourButton('first', 'First'), second);
        expect(service.start(tour)).toBe(true);
        second.hidden = true;

        service.next();

        expect(service.activeTour()).toBeNull();
        expect(service.target()).toBeNull();
        expect(storage.isCompleted(tour.id, tour.version)).toBe(true);
    });

    it('offers new tour versions and permits a fresh run when completion is cleared', () => {
        const tour = createTour();
        const newerTour = { ...tour, version: tour.version + 1 };
        host.append(createTourButton('first', 'First'));
        expect(service.start(tour)).toBe(true);
        service.skip();
        expect(service.start(tour)).toBe(false);

        expect(service.start(newerTour)).toBe(true);
        service.close();
        expect(storage.isCompleted(tour.id, tour.version)).toBe(true);
        expect(storage.isCompleted(tour.id, newerTour.version)).toBe(false);
        service.resetCompletion(tour.id);
        expect(service.start(tour)).toBe(true);
        expect(service.activeStep()?.id).toBe('first');
    });
});

describe('FdTourService server rendering', () => {
    it('does not query targets, prepare an overlay or persist a completion on the server', () => {
        const storage = new MemoryTourStorage();
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'server' },
                { provide: FD_TOUR_STORAGE, useValue: storage },
            ],
        });
        const service = TestBed.inject(FdTourService);
        const selectorQuery = vi.spyOn(TestBed.inject(DOCUMENT), 'querySelectorAll');
        const tour = createTour();
        try {
            expect(service.start(tour, { force: true })).toBe(false);
            service.refreshTarget();
            TestBed.tick();
            expect(selectorQuery).not.toHaveBeenCalled();
            expect(service.activeTour()).toBeNull();
            expect(service.preparing()).toBe(false);
            expect(storage.isCompleted(tour.id, tour.version)).toBe(false);
        } finally {
            selectorQuery.mockRestore();
        }
    });
});

describe('FdTourService refresh and dynamic containers', () => {
    let service: FdTourService;
    let host: HTMLElement;
    let registry: FdTourTargetRegistry;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FdTourHostComponent],
            providers: [{ provide: FD_TOUR_STORAGE, useValue: new MemoryTourStorage() }],
        });
        service = TestBed.inject(FdTourService);
        registry = TestBed.inject(FdTourTargetRegistry);
        host = document.createElement('div');
        document.body.append(host);
    });

    afterEach(() => {
        service.close();
        host.remove();
    });

    it('does not activate a step when the tour is closed before a queued viewport refresh', () => {
        const fixture = TestBed.createComponent(FdTourHostComponent);
        host.append(createTourButton('first', 'First'));
        expect(service.start(createTour())).toBe(true);
        fixture.detectChanges();

        service.refreshTarget();
        service.close();
        fixture.detectChanges();

        expect(service.activeStep()).toBeNull();
        expect(service.target()).toBeNull();
        expect(service.preparing()).toBe(false);
    });

    it('lets an in-progress reveal finish before reacting to a queued viewport refresh', () => {
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const action = createTourButton('first', 'First');
        const cleanup = vi.fn(action.remove.bind(action));
        const reveal = vi.fn(() => {
            host.append(action);
            return cleanup;
        });
        registry.register({ canReveal: selector => selector === '[data-tour-id="first"]', reveal });

        service.refreshTarget();
        expect(service.start(createTour())).toBe(true);
        expect(service.preparing()).toBe(true);
        service.refreshTarget();
        fixture.detectChanges();

        expect(service.preparing()).toBe(false);
        expect(service.target()).toBe(action);
        expect(reveal).toHaveBeenCalledOnce();
        expect(cleanup).not.toHaveBeenCalled();
        service.close();
        expect(cleanup).toHaveBeenCalledOnce();
    });

    it('moves past a nested target whose container is disposed when leaving the previous step', () => {
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const last = createTourButton('last', 'Last');
        host.append(last);
        const revealMiddle = vi.fn(() => vi.fn());
        const unregisterMiddle = registry.register({
            canReveal: selector => selector === '[data-tour-id="middle"]',
            reveal: revealMiddle,
        });
        registry.register({
            canReveal: selector => selector === '[data-tour-id="first"]',
            reveal: () => {
                const first = createTourButton('first', 'First');
                host.append(first);
                return (): void => {
                    first.remove();
                    unregisterMiddle();
                };
            },
        });
        expect(service.start(createThreeStepsTour())).toBe(true);
        fixture.detectChanges();
        expect(service.activeStep()?.id).toBe('first');

        service.next();

        expect(service.target()).toBe(last);
        expect(service.snapshot()?.isLastStep).toBe(true);
        expect(revealMiddle).not.toHaveBeenCalled();
    });
});

it('does not advance after a resolver cleanup closes the tour while destroying its owner', () => {
    TestBed.configureTestingModule({
        imports: [FdTourHostComponent, TourDrawerFixtureComponent],
        providers: [{ provide: FD_TOUR_STORAGE, useValue: new MemoryTourStorage() }],
    });
    const service = TestBed.inject(FdTourService);
    const fixture = TestBed.createComponent(FdTourHostComponent);
    const next = createTourButton('second', 'Second');
    const nextScroll = vi.fn();
    Object.defineProperty(next, 'scrollIntoView', { value: nextScroll });
    document.body.append(next);
    TestBed.inject(FdTourTargetRegistry).register({
        canReveal: selector => selector === '[data-tour-id="first"]',
        reveal: () => {
            const drawer = TestBed.createComponent(TourDrawerFixtureComponent);
            drawer.componentRef.onDestroy(() => {
                service.close();
            });
            drawer.detectChanges();
            return (): void => {
                drawer.destroy();
            };
        },
    });

    try {
        expect(service.start(createTour())).toBe(true);
        fixture.detectChanges();
        expect(service.target()?.textContent).toBe('First');

        service.next();

        expect(service.snapshot()).toBeNull();
        expect(service.target()).toBeNull();
        expect(nextScroll).not.toHaveBeenCalled();
    } finally {
        service.close();
        next.remove();
    }
});

function createThreeStepsTour(): FdTourDefinition {
    return {
        id: 'three-step-tour',
        version: 1,
        steps: [
            { id: 'first', target: '[data-tour-id="first"]', title: 'First' },
            { id: 'middle', target: '[data-tour-id="middle"]', title: 'Middle' },
            { id: 'last', target: '[data-tour-id="last"]', title: 'Last' },
        ],
    };
}
