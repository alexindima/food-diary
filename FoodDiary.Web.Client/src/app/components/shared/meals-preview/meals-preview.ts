import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import type { AiInputBarResult } from '../ai-input-bar/ai-input-bar.types';
import type { MealCardItem } from '../meal-card/meal-card';
import { MealsPreviewEntryComponent } from './meals-preview-entry/meals-preview-entry';
import type { MealPreviewEntry } from './meals-preview-lib/meals-preview.types';

@Component({
    selector: 'fd-meals-preview',
    imports: [TranslateModule, FdUiButtonComponent, MealsPreviewEntryComponent],
    templateUrl: './meals-preview.html',
    styleUrls: ['./meals-preview.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealsPreviewComponent {
    public readonly titleKey = input<string>('DASHBOARD.MEALS_TITLE');
    public readonly titleText = input<string | null>(null);
    public readonly viewAllKey = input<string>('DASHBOARD.MEALS_VIEW_ALL');
    public readonly emptyKey = input<string>('DASHBOARD.MEALS_EMPTY');
    public readonly showViewAll = input<boolean>(true);
    public readonly showAddButtons = input<boolean>(true);
    public readonly showAiButtons = input<boolean>(false);
    public readonly showEmptyState = input<boolean>(true);
    public readonly isAiMealSaving = input(false);
    public readonly aiMealClearToken = input(0);
    public readonly entries = input.required<MealPreviewEntry[]>();

    public readonly viewAll = output();
    public readonly add = output<string | null | undefined>();
    public readonly aiMealCreateRequested = output<AiInputBarResult>();
    public readonly open = output<MealCardItem>();
    protected readonly expandedAiSlot = signal<string | null>(null);

    public constructor() {
        effect(() => {
            if (this.aiMealClearToken() > 0) {
                this.expandedAiSlot.set(null);
            }
        });
    }

    protected toggleAi(slot: string | null = null): void {
        this.expandedAiSlot.update(current => (current === slot ? null : slot));
    }

    protected handleAiMealCreateRequested(result: AiInputBarResult): void {
        this.aiMealCreateRequested.emit(result);
    }
}
