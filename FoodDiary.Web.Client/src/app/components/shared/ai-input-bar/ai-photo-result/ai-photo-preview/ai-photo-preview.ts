import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    type ElementRef,
    input,
    output,
    signal,
    viewChild,
    viewChildren,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import {
    evaluateAiPhotoAnnotationLayout,
    fitAiPhotoAnnotationLayout,
    resolveAiPhotoAnnotationConnectorPath,
} from '../ai-photo-result-lib/ai-photo-annotation-layout';
import type { AiPhotoAnnotation } from '../ai-photo-result-lib/ai-photo-result.types';

const DEFAULT_IMAGE_ASPECT_RATIO = 1.5;
const FRAME_PERCENT = 100;

@Component({
    selector: 'fd-ai-photo-preview',
    imports: [TranslatePipe],
    templateUrl: './ai-photo-preview.html',
    styleUrl: '../ai-photo-result.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoPreviewComponent {
    private readonly expandedAnnotationLimit = 6;
    private readonly annotationFrame = viewChild<ElementRef<HTMLElement>>('annotationFrame');
    private readonly desktopCards = viewChildren<ElementRef<HTMLButtonElement>>('desktopCard');
    private readonly mobileCards = viewChildren<ElementRef<HTMLButtonElement>>('mobileCard');

    public readonly imageUrl = input.required<string | null>();
    public readonly sourceText = input.required<string | null>();
    public readonly sourceTextLabelKey = input.required<string>();
    public readonly isAnalyzing = input.required<boolean>();
    public readonly isPreparing = input(false);
    public readonly isNutritionLoading = input.required<boolean>();
    public readonly annotations = input<readonly AiPhotoAnnotation[]>([]);
    public readonly mobileAnnotations = input<readonly AiPhotoAnnotation[]>([]);
    public readonly annotationsVisible = input(true);
    public readonly activeAnnotationId = input<string | null>(null);
    public readonly annotationsToggled = output();
    public readonly annotationSelected = output<string>();
    public readonly imageOrientationChanged = output<boolean>();
    protected readonly imageAspectRatio = signal(DEFAULT_IMAGE_ASPECT_RATIO);
    protected readonly desktopConnectorPaths = signal<ReadonlyMap<string, string>>(new Map());
    protected readonly mobileConnectorPaths = signal<ReadonlyMap<string, string>>(new Map());

    protected readonly usesCompactAnnotations = computed(() => this.annotations().length > this.expandedAnnotationLimit);
    protected readonly activeAnnotation = computed(
        () => this.annotations().find(annotation => annotation.id === this.activeAnnotationId()) ?? this.annotations().at(0),
    );
    protected readonly cardAnnotations = computed(() => {
        if (!this.usesCompactAnnotations()) {
            const annotations = this.annotations();
            return evaluateAiPhotoAnnotationLayout(annotations).cardOverlaps === 0
                ? annotations
                : fitAiPhotoAnnotationLayout(annotations, this.activeAnnotationId());
        }

        const active = this.activeAnnotation();
        return active === undefined ? [] : [active];
    });
    protected readonly markerAnnotations = computed(() => {
        const cardIds = new Set(this.cardAnnotations().map(annotation => annotation.id));
        return this.annotations()
            .map((annotation, index) => ({ annotation, number: index + 1 }))
            .filter(item => !cardIds.has(item.annotation.id));
    });
    protected readonly mobileCardAnnotations = computed(() => {
        return this.mobileAnnotations();
    });
    protected readonly mobileMarkerAnnotations = computed(() => {
        const cardIds = new Set(this.mobileCardAnnotations().map(annotation => annotation.id));
        return this.annotations()
            .map((annotation, index) => ({ annotation, number: index + 1 }))
            .filter(item => !cardIds.has(item.annotation.id));
    });

    public constructor() {
        afterRenderEffect(onCleanup => {
            const frame = this.annotationFrame()?.nativeElement;
            const desktopCards = this.desktopCards().map(card => card.nativeElement);
            const mobileCards = this.mobileCards().map(card => card.nativeElement);
            const desktopAnnotations = this.cardAnnotations();
            const mobileAnnotations = this.mobileCardAnnotations();
            if (frame === undefined) {
                return;
            }
            const update = (): void => {
                this.desktopConnectorPaths.set(this.measureConnectorPaths(frame, desktopCards, desktopAnnotations));
                this.mobileConnectorPaths.set(this.measureConnectorPaths(frame, mobileCards, mobileAnnotations));
            };
            update();
            const Observer = frame.ownerDocument.defaultView?.ResizeObserver;
            if (Observer !== undefined) {
                const observer = new Observer(update);
                for (const element of [frame, ...desktopCards, ...mobileCards]) {
                    observer.observe(element);
                }
                onCleanup(() => {
                    observer.disconnect();
                });
            }
        });
    }

    private measureConnectorPaths(
        frame: HTMLElement,
        cards: readonly HTMLButtonElement[],
        annotations: readonly AiPhotoAnnotation[],
    ): ReadonlyMap<string, string> {
        const bounds = frame.getBoundingClientRect();
        const paths = new Map<string, string>();
        if (bounds.width === 0 || bounds.height === 0) {
            return paths;
        }
        for (const [index, card] of cards.entries()) {
            const annotation = annotations.at(index);
            const rect = card.getBoundingClientRect();
            if (annotation === undefined || rect.width === 0 || rect.height === 0) {
                continue;
            }
            paths.set(
                annotation.id,
                resolveAiPhotoAnnotationConnectorPath(annotation, {
                    x: ((rect.left - bounds.left) / bounds.width) * FRAME_PERCENT,
                    y: ((rect.top - bounds.top) / bounds.height) * FRAME_PERCENT,
                    width: (rect.width / bounds.width) * FRAME_PERCENT,
                    height: (rect.height / bounds.height) * FRAME_PERCENT,
                }),
            );
        }
        return paths;
    }

    protected onImageLoaded(event: Event): void {
        const image = event.currentTarget;
        if (image instanceof HTMLImageElement && image.naturalWidth > 0 && image.naturalHeight > 0) {
            const isPortrait = image.naturalHeight > image.naturalWidth;
            this.imageAspectRatio.set(image.naturalWidth / image.naturalHeight);
            this.imageOrientationChanged.emit(isPortrait);
        }
    }
}
