import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AiNutritionSummaryItem } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-nutrition-summary',
    imports: [TranslatePipe],
    templateUrl: './ai-photo-nutrition-summary.component.html',
    styleUrl: '../ai-photo-result.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoNutritionSummaryComponent {
    public readonly items = input.required<AiNutritionSummaryItem[]>();
}
