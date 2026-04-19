import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { MatIconModule } from '@angular/material/icon';
import { MealCardComponent, MealCardItem } from '../meal-card/meal-card.component';
import { AiInputBarComponent } from '../ai-input-bar/ai-input-bar.component';

export interface MealPreviewEntry {
    meal?: MealCardItem | null;
    slot?: string | null;
    icon?: string;
    labelKey?: string;
}

@Component({
    selector: 'fd-meals-preview',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiButtonComponent, MatIconModule, MealCardComponent, AiInputBarComponent],
    templateUrl: './meals-preview.component.html',
    styleUrls: ['./meals-preview.component.scss'],
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
    public readonly entries = input.required<MealPreviewEntry[]>();

    public readonly viewAll = output<void>();
    public readonly add = output<string | null | undefined>();
    public readonly aiMealCreated = output<void>();
    public readonly open = output<MealCardItem>();
    public readonly expandedAiSlot = signal<string | null>(null);

    public toggleAi(slot?: string | null): void {
        const normalizedSlot = slot ?? null;
        this.expandedAiSlot.update(current => (current === normalizedSlot ? null : normalizedSlot));
    }

    public handleAiMealCreated(): void {
        this.expandedAiSlot.set(null);
        this.aiMealCreated.emit();
    }
}
