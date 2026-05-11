import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-ai-photo-preview',
    imports: [NgOptimizedImage, TranslatePipe],
    templateUrl: './ai-photo-preview.component.html',
    styleUrl: './ai-photo-result.component.scss',
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
}
