import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import { AiInputBarComponent } from '../ai-input-bar/ai-input-bar.component';
import { MealCardComponent, type MealCardItem } from '../meal-card/meal-card.component';
import type { MealPreviewEntry } from './meals-preview.component';

@Component({
    selector: 'fd-meals-preview-entry',
    imports: [TranslateModule, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent, MealCardComponent, AiInputBarComponent],
    templateUrl: './meals-preview-entry.component.html',
    styleUrl: './meals-preview.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealsPreviewEntryComponent {
    public readonly entry = input.required<MealPreviewEntry>();
    public readonly showAddButtons = input.required<boolean>();
    public readonly showAiButtons = input.required<boolean>();
    public readonly expandedAiSlot = input<string | null>(null);

    public readonly open = output<MealCardItem>();
    public readonly add = output<string | null | undefined>();
    public readonly aiToggle = output<string | null | undefined>();
    public readonly aiMealCreated = output();
}
