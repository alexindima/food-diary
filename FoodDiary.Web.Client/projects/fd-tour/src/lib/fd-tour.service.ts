import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { computed, inject, PLATFORM_ID, Service, signal } from '@angular/core';

import type { FdTourDefinition, FdTourSnapshot, FdTourStartOptions, FdTourStep } from './fd-tour.types';
import { FD_TOUR_STORAGE } from './fd-tour-storage';

type ActiveTour = {
    tour: FdTourDefinition;
    steps: readonly FdTourStep[];
};

@Service()
export class FdTourService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly storage = inject(FD_TOUR_STORAGE);
    private readonly activeTourState = signal<ActiveTour | null>(null);
    private readonly activeStepIndexState = signal(0);

    public readonly activeTour = computed(() => this.activeTourState()?.tour ?? null);
    public readonly activeStep = computed(() => this.snapshot()?.step ?? null);
    public readonly snapshot = computed<FdTourSnapshot | null>(() => {
        const activeTour = this.activeTourState();
        if (activeTour === null) {
            return null;
        }

        const stepIndex = this.activeStepIndexState();
        const step = activeTour.steps[stepIndex];

        return {
            tour: activeTour.tour,
            step,
            stepIndex,
            stepCount: activeTour.steps.length,
            isFirstStep: stepIndex === 0,
            isLastStep: stepIndex === activeTour.steps.length - 1,
        };
    });

    public start(tour: FdTourDefinition, options: FdTourStartOptions = {}): boolean {
        if (!isPlatformBrowser(this.platformId)) {
            return false;
        }

        if (tour.steps.length === 0 || (options.force !== true && this.storage.isCompleted(tour.id, tour.version))) {
            return false;
        }

        const steps = tour.steps.filter(step => this.hasTarget(step));
        if (steps.length === 0) {
            return false;
        }

        this.activeTourState.set({ tour, steps });
        this.activeStepIndexState.set(0);
        this.scrollCurrentTargetIntoView();
        return true;
    }

    public next(): void {
        const snapshot = this.snapshot();
        if (snapshot === null) {
            return;
        }

        if (snapshot.isLastStep) {
            this.complete();
            return;
        }

        this.moveTo(snapshot.stepIndex + 1, 1);
    }

    public previous(): void {
        const snapshot = this.snapshot();
        if (snapshot === null || snapshot.isFirstStep) {
            return;
        }

        this.moveTo(snapshot.stepIndex - 1, -1);
    }

    public skip(): void {
        this.finish();
    }

    public complete(): void {
        this.finish();
    }

    public close(): void {
        this.reset();
    }

    public resetCompletion(tourId: string): void {
        this.storage.clearCompleted(tourId);
    }

    private moveTo(candidateIndex: number, direction: 1 | -1): void {
        const activeTour = this.activeTourState();
        if (activeTour === null) {
            return;
        }

        let nextIndex = candidateIndex;
        while (nextIndex >= 0 && nextIndex < activeTour.steps.length) {
            const step = activeTour.steps[nextIndex];
            if (this.hasTarget(step)) {
                this.activeStepIndexState.set(nextIndex);
                this.scrollCurrentTargetIntoView();
                return;
            }

            nextIndex += direction;
        }

        if (direction > 0) {
            this.complete();
        }
    }

    private scrollCurrentTargetIntoView(): void {
        const step = this.activeStep();
        const target = step === null ? null : this.findTarget(step);
        if (target instanceof HTMLElement && typeof target.scrollIntoView === 'function') {
            target.scrollIntoView({ block: 'center', inline: 'center', behavior: 'smooth' });
        }
    }

    private hasTarget(step: FdTourStep): boolean {
        return this.findTarget(step) !== null;
    }

    private findTarget(step: FdTourStep): Element | null {
        if (!isPlatformBrowser(this.platformId)) {
            return null;
        }

        try {
            return this.document.querySelector(step.target);
        } catch {
            return null;
        }
    }

    private finish(): void {
        const activeTour = this.activeTourState();
        if (activeTour !== null) {
            this.storage.markCompleted(activeTour.tour.id, activeTour.tour.version);
        }

        this.reset();
    }

    private reset(): void {
        this.activeTourState.set(null);
        this.activeStepIndexState.set(0);
    }
}
