import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AiPhotoAnnotation } from '../ai-photo-result-lib/ai-photo-result.types';

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

    public readonly imageUrl = input.required<string | null>();
    public readonly sourceText = input.required<string | null>();
    public readonly sourceTextLabelKey = input.required<string>();
    public readonly isAnalyzing = input.required<boolean>();
    public readonly isPreparing = input(false);
    public readonly isNutritionLoading = input.required<boolean>();
    public readonly annotations = input<readonly AiPhotoAnnotation[]>([]);
    public readonly annotationsVisible = input(true);
    public readonly activeAnnotationId = input<string | null>(null);
    public readonly annotationsToggled = output();
    public readonly annotationSelected = output<string>();
    public readonly imageOrientationChanged = output<boolean>();
    protected readonly isPortrait = signal(false);
    protected readonly imageAspectRatio = signal('auto');

    protected readonly usesCompactAnnotations = computed(() => this.annotations().length > this.expandedAnnotationLimit);
    protected readonly activeAnnotation = computed(
        () => this.annotations().find(annotation => annotation.id === this.activeAnnotationId()) ?? this.annotations().at(0),
    );
    protected readonly cardAnnotations = computed(() => {
        if (!this.usesCompactAnnotations()) {
            return this.annotations();
        }

        const active = this.activeAnnotation();
        return active === undefined ? [] : [active];
    });
    protected readonly markerAnnotations = computed(() =>
        this.usesCompactAnnotations()
            ? this.annotations()
                  .map((annotation, index) => ({ annotation, number: index + 1 }))
                  .filter(item => item.annotation.id !== this.activeAnnotation()?.id)
            : [],
    );

    protected onImageLoaded(event: Event): void {
        const image = event.currentTarget;
        if (image instanceof HTMLImageElement) {
            const isPortrait = image.naturalHeight > image.naturalWidth;
            this.isPortrait.set(isPortrait);
            this.imageAspectRatio.set(`${image.naturalWidth} / ${image.naturalHeight}`);
            this.imageOrientationChanged.emit(isPortrait);
        }
    }
}
