import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { afterNextRender, computed, DestroyRef, effect, inject, Injector, PLATFORM_ID, Service, signal, untracked } from '@angular/core';

import type { FdTourDefinition, FdTourSnapshot, FdTourStartOptions, FdTourStep } from './fd-tour.types';
import { FD_TOUR_STORAGE } from './fd-tour-storage';
import { findTourTarget } from './fd-tour-target';
import { FdTourTargetRegistry, type FdTourTargetResolver } from './fd-tour-target-registry';

type ActiveTour = {
    tour: FdTourDefinition;
    steps: readonly FdTourStep[];
};

@Service()
export class FdTourService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly storage = inject(FD_TOUR_STORAGE);
    private readonly registry = inject(FdTourTargetRegistry);
    private readonly injector = inject(Injector);
    private readonly destroyRef = inject(DestroyRef);
    private readonly activeTourState = signal<ActiveTour | null>(null);
    private readonly activeStepIndexState = signal(0);
    private readonly targetState = signal<Element | null>(null);
    private readonly preparingState = signal(false);
    private cleanupTarget: (() => void) | null = null;
    private activeResolver: FdTourTargetResolver | null = null;
    private preparationRevision = 0;
    private refreshScheduled = false;

    public readonly target = this.targetState.asReadonly();
    public readonly preparing = this.preparingState.asReadonly();

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

    public constructor() {
        effect(() => {
            const resolvers = this.registry.resolvers();
            if (this.activeResolver !== null && !resolvers.includes(this.activeResolver)) {
                untracked(() => {
                    this.close();
                });
            }
        });
        this.destroyRef.onDestroy(() => {
            this.releaseTarget();
        });
    }

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

        this.releaseTarget();
        this.activeTourState.set({ tour, steps });
        this.activeStepIndexState.set(0);
        this.prepareCurrentTarget();
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
                this.prepareCurrentTarget();
                return;
            }

            nextIndex += direction;
        }

        if (direction > 0) {
            this.complete();
        }
    }

    private scrollCurrentTargetIntoView(block: ScrollLogicalPosition = 'center'): void {
        const target = this.target();
        if (target instanceof HTMLElement && typeof target.scrollIntoView === 'function') {
            const viewport = this.document.defaultView;
            const reducedMotion =
                viewport !== null &&
                typeof viewport.matchMedia === 'function' &&
                viewport.matchMedia('(prefers-reduced-motion: reduce)').matches;
            target.scrollIntoView({ block, inline: 'center', behavior: reducedMotion ? 'instant' : 'smooth' });
        }
    }

    private hasTarget(step: FdTourStep): boolean {
        return this.findTarget(step) !== null || this.registry.find(step.target) !== undefined;
    }

    /** Re-evaluate responsive conditions after a viewport or container change. */
    public refreshTarget(): void {
        if (this.refreshScheduled || !isPlatformBrowser(this.platformId)) {
            return;
        }
        this.refreshScheduled = true;
        afterNextRender(
            () => {
                this.refreshScheduled = false;
                this.refreshRenderedTarget();
            },
            { injector: this.injector },
        );
    }

    private refreshRenderedTarget(): void {
        if (this.preparing()) {
            return;
        }
        const step = this.activeStep();
        if (step === null) {
            return;
        }
        const target = this.findTarget(step);
        if (target !== null && target === this.target()) {
            return;
        }
        this.prepareCurrentTarget(false);
    }

    private prepareCurrentTarget(skipMissing = true): void {
        this.releaseTarget();
        const step = this.activeStep();
        if (step === null) {
            return;
        }
        const visible = this.findTarget(step);
        if (visible !== null) {
            this.targetState.set(visible);
            this.scrollCurrentTargetIntoView();
            return;
        }
        const resolver = this.registry.find(step.target);
        if (resolver === undefined) {
            if (skipMissing) {
                this.next();
            }
            return;
        }
        this.preparingState.set(true);
        this.activeResolver = resolver;
        this.cleanupTarget = resolver.reveal(step.target);
        const revision = this.preparationRevision;
        const renderRef = afterNextRender(
            () => {
                if (revision !== this.preparationRevision) {
                    return;
                }
                this.preparingState.set(false);
                const target = this.findTarget(step);
                if (target === null) {
                    this.next();
                    return;
                }
                this.targetState.set(target);
                this.scrollCurrentTargetIntoView('nearest');
            },
            { injector: this.injector },
        );
        const cleanup = this.cleanupTarget;
        this.cleanupTarget = (): void => {
            renderRef.destroy();
            cleanup();
        };
    }

    private releaseTarget(): void {
        this.preparationRevision += 1;
        this.cleanupTarget?.();
        this.cleanupTarget = null;
        this.activeResolver = null;
        this.preparingState.set(false);
        this.targetState.set(null);
    }

    private findTarget(step: FdTourStep): Element | null {
        if (!isPlatformBrowser(this.platformId)) {
            return null;
        }

        return findTourTarget(this.document, step.target);
    }

    private finish(): void {
        const activeTour = this.activeTourState();
        if (activeTour !== null) {
            this.storage.markCompleted(activeTour.tour.id, activeTour.tour.version);
        }

        this.reset();
    }

    private reset(): void {
        this.releaseTarget();
        this.activeTourState.set(null);
        this.activeStepIndexState.set(0);
    }
}
