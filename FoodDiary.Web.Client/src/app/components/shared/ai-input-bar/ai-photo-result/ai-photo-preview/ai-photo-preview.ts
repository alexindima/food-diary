import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AiPhotoAnnotation } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-preview',
    imports: [NgOptimizedImage, TranslatePipe],
    templateUrl: './ai-photo-preview.html',
    styleUrl: '../ai-photo-result.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoPreviewComponent {
    public readonly imageUrl = input.required<string | null>();
    public readonly sourceText = input.required<string | null>();
    public readonly sourceTextLabelKey = input.required<string>();
    public readonly isAnalyzing = input.required<boolean>();
    public readonly isNutritionLoading = input.required<boolean>();
    public readonly annotations = input<readonly AiPhotoAnnotation[]>([]);
    public readonly annotationsVisible = input(true);
    public readonly annotationsToggled = output();
}
