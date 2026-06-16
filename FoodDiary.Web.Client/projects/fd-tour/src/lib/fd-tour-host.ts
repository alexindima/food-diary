import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    PLATFORM_ID,
    signal,
    viewChild,
} from '@angular/core';

import { FdTourService } from './fd-tour.service';
import type { FdTourLabels, FdTourPlacement, FdTourStep } from './fd-tour.types';

type TourRect = {
    top: number;
    left: number;
    width: number;
    height: number;
};

type PopoverPosition = {
    top: number;
    left: number;
};

type PopoverLayout = PopoverPosition & {
    width: number;
};

type PopoverSize = {
    width: number;
    height: number;
};

const DEFAULT_LABELS: FdTourLabels = {
    previous: 'Back',
    next: 'Next',
    finish: 'Done',
    skip: 'Skip',
    close: 'Close',
};

const HIGHLIGHT_PADDING_PX = 8;
const POPOVER_WIDTH_PX = 360;
const POPOVER_MIN_WIDTH_PX = 280;
const POPOVER_ESTIMATED_HEIGHT_PX = 220;
const POPOVER_GAP_PX = 14;
const VIEWPORT_GUTTER_PX = 16;
const MOBILE_BREAKPOINT_PX = 640;
const SCROLL_IDLE_DELAY_MS = 140;
const STEP_REVEAL_FALLBACK_MS = 700;

@Component({
    selector: 'fd-tour-host',
    templateUrl: './fd-tour-host.html',
    styleUrl: './fd-tour-host.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdTourHostComponent {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly destroyRef = inject(DestroyRef);
    private readonly popover = viewChild<ElementRef<HTMLElement>>('popover');
    private presentedStepKey: string | null = null;
    private cleanupPopoverReveal: (() => void) | null = null;
    protected readonly tour = inject(FdTourService);
    protected readonly targetRect = signal<TourRect | null>(null);
    protected readonly popoverSize = signal<PopoverSize>({
        width: POPOVER_WIDTH_PX,
        height: POPOVER_ESTIMATED_HEIGHT_PX,
    });
    protected readonly popoverVisible = signal(false);
    protected readonly labels = computed<FdTourLabels>(() => ({
        ...DEFAULT_LABELS,
        ...this.tour.activeTour()?.labels,
    }));
    protected readonly highlightRect = computed<TourRect | null>(() => {
        const rect = this.targetRect();
        if (rect === null) {
            return null;
        }

        return {
            top: Math.max(rect.top - HIGHLIGHT_PADDING_PX, 0),
            left: Math.max(rect.left - HIGHLIGHT_PADDING_PX, 0),
            width: rect.width + HIGHLIGHT_PADDING_PX * 2,
            height: rect.height + HIGHLIGHT_PADDING_PX * 2,
        };
    });
    protected readonly popoverLayout = computed<PopoverLayout>(() => {
        const rect = this.highlightRect();
        const viewport = this.getViewport();
        if (rect === null || viewport === null) {
            return {
                top: VIEWPORT_GUTTER_PX,
                left: VIEWPORT_GUTTER_PX,
                width: POPOVER_WIDTH_PX,
            };
        }

        if (viewport.innerWidth <= MOBILE_BREAKPOINT_PX) {
            return {
                top: Math.max(viewport.innerHeight - this.popoverSize().height - VIEWPORT_GUTTER_PX, VIEWPORT_GUTTER_PX),
                left: VIEWPORT_GUTTER_PX,
                width: viewport.innerWidth - VIEWPORT_GUTTER_PX * 2,
            };
        }

        return this.placePopover(rect, this.tour.activeStep()?.placement ?? 'auto', viewport);
    });
    protected readonly progressLabel = computed(() => {
        const snapshot = this.tour.snapshot();
        if (snapshot === null) {
            return '';
        }

        return `${snapshot.stepIndex + 1} / ${snapshot.stepCount}`;
    });

    public constructor() {
        effect(() => {
            const snapshot = this.tour.snapshot();
            const stepKey = snapshot === null ? null : `${snapshot.tour.id}:${snapshot.stepIndex}:${snapshot.step.id}`;
            if (stepKey !== this.presentedStepKey) {
                this.presentedStepKey = stepKey;
                this.beginStepPresentation(stepKey, snapshot?.step ?? null);
                return;
            }

            this.measureTarget(snapshot?.step ?? null);
            this.schedulePopoverMeasurement();
        });

        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        const handleReposition = (): void => {
            this.measureTarget(this.tour.activeStep());
            this.schedulePopoverMeasurement();
        };
        const handleKeydown = (event: KeyboardEvent): void => {
            this.handleKeydown(event);
        };

        this.getViewport()?.addEventListener('resize', handleReposition, { passive: true });
        this.document.addEventListener('scroll', handleReposition, { capture: true, passive: true });
        this.document.addEventListener('keydown', handleKeydown);
        this.destroyRef.onDestroy(() => {
            this.cleanupPopoverReveal?.();
            this.getViewport()?.removeEventListener('resize', handleReposition);
            this.document.removeEventListener('scroll', handleReposition, { capture: true });
            this.document.removeEventListener('keydown', handleKeydown);
        });
    }

    protected close(): void {
        this.tour.close();
    }

    protected skip(): void {
        this.tour.skip();
    }

    protected previous(): void {
        this.tour.previous();
    }

    protected next(): void {
        this.tour.next();
    }

    private handleKeydown(event: KeyboardEvent): void {
        if (this.tour.snapshot() === null) {
            return;
        }

        switch (event.key) {
            case 'Escape': {
                this.tour.close();
                break;
            }
            case 'ArrowRight': {
                this.tour.next();
                break;
            }
            case 'ArrowLeft': {
                this.tour.previous();
                break;
            }
        }
    }

    private measureTarget(step: FdTourStep | null): void {
        if (step === null || !isPlatformBrowser(this.platformId)) {
            this.targetRect.set(null);
            return;
        }

        const element = this.document.querySelector(step.target);
        const rect = element?.getBoundingClientRect();
        if (rect === undefined) {
            this.targetRect.set(null);
            queueMicrotask(() => {
                this.tour.next();
            });
            return;
        }

        this.targetRect.set({
            top: rect.top,
            left: rect.left,
            width: rect.width,
            height: rect.height,
        });
    }

    private beginStepPresentation(stepKey: string | null, step: FdTourStep | null): void {
        this.cleanupPopoverReveal?.();
        this.popoverVisible.set(false);
        this.measureTarget(step);
        this.schedulePopoverMeasurement();

        if (stepKey === null || step === null) {
            return;
        }

        this.schedulePopoverReveal(stepKey);
    }

    private schedulePopoverReveal(stepKey: string): void {
        if (!isPlatformBrowser(this.platformId)) {
            this.popoverVisible.set(true);
            return;
        }

        const viewport = this.getViewport();
        if (viewport === null) {
            this.popoverVisible.set(true);
            return;
        }

        let idleTimeoutId: number | null = null;
        let fallbackTimeoutId: number | null = null;
        let revealed = false;

        const cleanup = (): void => {
            this.document.removeEventListener('scroll', handleScroll, { capture: true });
            if (idleTimeoutId !== null) {
                viewport.clearTimeout(idleTimeoutId);
            }
            if (fallbackTimeoutId !== null) {
                viewport.clearTimeout(fallbackTimeoutId);
            }
            if (this.cleanupPopoverReveal === cleanup) {
                this.cleanupPopoverReveal = null;
            }
        };

        const reveal = (): void => {
            if (revealed) {
                return;
            }

            revealed = true;
            cleanup();
            if (this.presentedStepKey !== stepKey) {
                return;
            }

            this.measureTarget(this.tour.activeStep());
            this.schedulePopoverMeasurement();
            viewport.requestAnimationFrame(() => {
                this.measurePopover();
                viewport.requestAnimationFrame(() => {
                    if (this.presentedStepKey === stepKey) {
                        this.popoverVisible.set(true);
                    }
                });
            });
        };

        const scheduleIdleReveal = (): void => {
            if (idleTimeoutId !== null) {
                viewport.clearTimeout(idleTimeoutId);
            }
            idleTimeoutId = viewport.setTimeout(reveal, SCROLL_IDLE_DELAY_MS);
        };

        const handleScroll = (): void => {
            scheduleIdleReveal();
        };

        this.document.addEventListener('scroll', handleScroll, { capture: true, passive: true });
        this.cleanupPopoverReveal = cleanup;
        scheduleIdleReveal();
        fallbackTimeoutId = viewport.setTimeout(reveal, STEP_REVEAL_FALLBACK_MS);
    }

    private placePopover(rect: TourRect, preferredPlacement: FdTourPlacement, viewport: Window): PopoverLayout {
        const size = this.popoverSize();
        const placement = this.resolvePlacement(rect, preferredPlacement, viewport, size);
        const placementSize = {
            ...size,
            width: this.resolvePopoverWidth(rect, placement, viewport),
        };

        switch (placement) {
            case 'top': {
                return this.fitToViewport(
                    {
                        top: rect.top - placementSize.height - POPOVER_GAP_PX,
                        left: rect.left + rect.width / 2 - placementSize.width / 2,
                    },
                    placementSize,
                );
            }
            case 'right': {
                return this.fitToViewport(
                    {
                        top: rect.top,
                        left: rect.left + rect.width + POPOVER_GAP_PX,
                    },
                    placementSize,
                );
            }
            case 'left': {
                return this.fitToViewport(
                    {
                        top: rect.top,
                        left: rect.left - placementSize.width - POPOVER_GAP_PX,
                    },
                    placementSize,
                );
            }
            case 'bottom':
            case 'auto': {
                return this.fitToViewport(
                    {
                        top: rect.top + rect.height + POPOVER_GAP_PX,
                        left: rect.left + rect.width / 2 - placementSize.width / 2,
                    },
                    placementSize,
                );
            }
        }
    }

    private resolvePlacement(rect: TourRect, preferredPlacement: FdTourPlacement, viewport: Window, size: PopoverSize): FdTourPlacement {
        const placements: FdTourPlacement[] =
            preferredPlacement === 'auto' ? ['bottom', 'top', 'right', 'left'] : [preferredPlacement, 'bottom', 'top', 'right', 'left'];

        for (const placement of placements) {
            if (placement !== 'auto' && this.hasPlacementSpace(rect, placement, viewport, size)) {
                return placement;
            }
        }

        return preferredPlacement === 'auto' ? 'bottom' : preferredPlacement;
    }

    private hasPlacementSpace(rect: TourRect, placement: Exclude<FdTourPlacement, 'auto'>, viewport: Window, size: PopoverSize): boolean {
        switch (placement) {
            case 'top': {
                return rect.top - size.height - POPOVER_GAP_PX >= VIEWPORT_GUTTER_PX;
            }
            case 'right': {
                return this.getAvailableRightWidth(rect, viewport) >= POPOVER_MIN_WIDTH_PX;
            }
            case 'bottom': {
                return rect.top + rect.height + POPOVER_GAP_PX + size.height <= viewport.innerHeight - VIEWPORT_GUTTER_PX;
            }
            case 'left': {
                return this.getAvailableLeftWidth(rect) >= POPOVER_MIN_WIDTH_PX;
            }
        }
    }

    private resolvePopoverWidth(rect: TourRect, placement: FdTourPlacement, viewport: Window): number {
        switch (placement) {
            case 'right': {
                return Math.min(POPOVER_WIDTH_PX, this.getAvailableRightWidth(rect, viewport));
            }
            case 'left': {
                return Math.min(POPOVER_WIDTH_PX, this.getAvailableLeftWidth(rect));
            }
            case 'top':
            case 'bottom':
            case 'auto': {
                return Math.min(POPOVER_WIDTH_PX, viewport.innerWidth - VIEWPORT_GUTTER_PX * 2);
            }
        }
    }

    private getAvailableRightWidth(rect: TourRect, viewport: Window): number {
        return viewport.innerWidth - (rect.left + rect.width + POPOVER_GAP_PX) - VIEWPORT_GUTTER_PX;
    }

    private getAvailableLeftWidth(rect: TourRect): number {
        return rect.left - POPOVER_GAP_PX - VIEWPORT_GUTTER_PX;
    }

    private fitToViewport(position: PopoverPosition, size: PopoverSize): PopoverLayout {
        const viewport = this.getViewport();
        if (viewport === null) {
            return {
                ...position,
                width: size.width,
            };
        }

        const maxLeft = Math.max(viewport.innerWidth - size.width - VIEWPORT_GUTTER_PX, VIEWPORT_GUTTER_PX);
        const maxTop = Math.max(viewport.innerHeight - size.height - VIEWPORT_GUTTER_PX, VIEWPORT_GUTTER_PX);

        return {
            top: Math.min(Math.max(position.top, VIEWPORT_GUTTER_PX), maxTop),
            left: Math.min(Math.max(position.left, VIEWPORT_GUTTER_PX), maxLeft),
            width: size.width,
        };
    }

    private schedulePopoverMeasurement(): void {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }

        const viewport = this.getViewport();
        if (viewport === null || typeof viewport.requestAnimationFrame !== 'function') {
            return;
        }

        viewport.requestAnimationFrame(() => {
            this.measurePopover();
        });
    }

    private measurePopover(): void {
        const element = this.popover()?.nativeElement;
        if (element === undefined) {
            return;
        }

        const rect = element.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) {
            return;
        }

        this.popoverSize.set({
            width: rect.width,
            height: rect.height,
        });
    }

    private getViewport(): Window | null {
        return this.document.defaultView;
    }
}
