import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { MealDetailsFieldsComponent } from '../../../meal-details-fields/meal-details-fields.component';
import type { AiDetailsToggleView } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-details-panel',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, MealDetailsFieldsComponent],
    templateUrl: './ai-photo-details-panel.component.html',
    styleUrl: '../ai-photo-result.component.scss',
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
    public readonly date = input.required<string>();
    public readonly time = input.required<string>();
    public readonly comment = input.required<string>();
    public readonly preMealSatietyLevel = input.required<number | null>();
    public readonly postMealSatietyLevel = input.required<number | null>();

    public readonly detailsToggle = output();
    public readonly mealSubmit = output();
    public readonly dateChange = output<string>();
    public readonly timeChange = output<string>();
    public readonly commentChange = output<string>();
    public readonly preMealSatietyLevelChange = output<number | null>();
    public readonly postMealSatietyLevelChange = output<number | null>();
}
