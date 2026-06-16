import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { FdTourService } from './fd-tour.service';
import type { FdTourDefinition } from './fd-tour.types';
import { FD_TOUR_STORAGE, type FdTourStorage } from './fd-tour-storage';

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

    beforeEach(() => {
        storage = new MemoryTourStorage();

        TestBed.configureTestingModule({
            providers: [{ provide: FD_TOUR_STORAGE, useValue: storage }],
        });

        const domDocument = TestBed.inject(DOCUMENT);
        host = domDocument.createElement('div');
        host.append(createTourButton('first', 'First'), createTourButton('second', 'Second'));
        domDocument.body.append(host);

        service = TestBed.inject(FdTourService);
    });

    afterEach(() => {
        host.remove();
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
