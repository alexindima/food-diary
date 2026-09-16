import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { MealDetailsFieldsComponent } from '../../../meal-details-fields/meal-details-fields';
import type { AiDetailsToggleView } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-details-panel',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, MealDetailsFieldsComponent],
    templateUrl: './ai-photo-details-panel.html',
    styleUrl: '../ai-photo-result.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoDetailsPanelComponent {
    public readonly isVisible = input.required<boolean>();
    public readonly showDetails = input.required<boolean>();
    public readonly isExpanded = input.required<boolean>();
    public readonly toggleView = input.required<AiDetailsToggleView>();
    public readonly submitLabelKey = input.required<string>();
    public readonly submitDisabled = input.required<boolean>();
    public readonly toggleDisabled = input.required<boolean>();
    public readonly date = model.required<string>();
    public readonly time = model.required<string>();
    public readonly comment = model.required<string>();
    public readonly preMealSatietyLevel = model.required<number | null>();
    public readonly postMealSatietyLevel = model.required<number | null>();

    public readonly detailsToggle = output();
    public readonly mealSubmit = output();
}
