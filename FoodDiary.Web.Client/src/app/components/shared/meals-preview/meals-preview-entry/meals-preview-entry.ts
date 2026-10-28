import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import { AiInputBarComponent } from '../../ai-input-bar/ai-input-bar';
import type { AiInputBarResult } from '../../ai-input-bar/ai-input-bar.types';
import { MealCardComponent, type MealCardItem } from '../../meal-card/meal-card';
import type { MealPreviewEntry } from '../meals-preview-lib/meals-preview.types';

let nextPanelId = 0;

@Component({
    selector: 'fd-meals-preview-entry',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, MealCardComponent, AiInputBarComponent],
    templateUrl: './meals-preview-entry.html',
    styleUrl: '../meals-preview.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealsPreviewEntryComponent {
    public readonly entry = input.required<MealPreviewEntry>();
    public readonly showAddButtons = input.required<boolean>();
    public readonly showAiButtons = input.required<boolean>();
    public readonly expandedAiSlot = input<string | null>(null);
    public readonly isAiMealSaving = input(false);
    public readonly aiMealClearToken = input(0);
    public readonly favoriteLoadingIds = input<ReadonlySet<string>>(new Set());
    public readonly favoriteToggle = output<{ id: string }>();

    public readonly open = output<MealCardItem>();
    public readonly add = output<string | null | undefined>();
    public readonly aiToggle = output<string | null | undefined>();
    public readonly aiMealCreateRequested = output<AiInputBarResult>();

    protected readonly panelId = `meal-preview-input-${nextPanelId++}`;
    protected readonly canExpand = computed(() => this.showAiButtons() && (this.entry().slot ?? null) !== null);
    protected readonly isExpanded = computed(() => this.canExpand() && this.expandedAiSlot() === this.entry().slot);
    protected readonly hasOpened = signal(false);

    public constructor() {
        effect(() => {
            if (this.isExpanded()) {
                this.hasOpened.set(true);
            }
        });
    }

    protected activate(): void {
        if (this.isAiMealSaving()) {
            return;
        }
        if (this.canExpand()) {
            this.aiToggle.emit(this.entry().slot);
        } else {
            this.add.emit(this.entry().slot);
        }
    }
}
