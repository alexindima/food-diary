import { ChangeDetectionStrategy, Component, effect, input, output, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import type { AiInputBarResult } from '../ai-input-bar/ai-input-bar.types';
import type { MealCardItem } from '../meal-card/meal-card';
import { MealsPreviewEntryComponent } from './meals-preview-entry/meals-preview-entry';
import type { MealPreviewEntry } from './meals-preview-lib/meals-preview.types';

@Component({
    selector: 'fd-meals-preview',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent, MealsPreviewEntryComponent],
    templateUrl: './meals-preview.html',
    styleUrls: ['./meals-preview.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealsPreviewComponent {
    private submittedAiSlot: string | null = null;
    private lastClearToken = 0;
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
    public readonly favoriteLoadingIds = input<ReadonlySet<string>>(new Set());
    public readonly favoriteToggle = output<{ id: string }>();
    public readonly entries = input.required<MealPreviewEntry[]>();

    public readonly viewAll = output();
    public readonly add = output<string | null | undefined>();
    public readonly aiMealCreateRequested = output<AiInputBarResult>();
    public readonly open = output<MealCardItem>();
    protected readonly expandedAiSlot = signal<string | null>(null);
    protected readonly aiMealClearTokens = signal<ReadonlyMap<string, number>>(new Map());

    public constructor() {
        effect(() => {
            const clearToken = this.aiMealClearToken();
            const saving = this.isAiMealSaving();
            untracked(() => {
                if (clearToken !== this.lastClearToken) {
                    this.lastClearToken = clearToken;
                    const slot = this.submittedAiSlot;
                    if (slot !== null) {
                        this.aiMealClearTokens.update(tokens => new Map(tokens).set(slot, clearToken));
                        if (this.expandedAiSlot() === slot) {
                            this.expandedAiSlot.set(null);
                        }
                    }
                    this.submittedAiSlot = null;
                } else if (!saving) {
                    this.submittedAiSlot = null;
                }
            });
        });
    }

    protected toggleAi(slot: string | null = null): void {
        if (this.isAiMealSaving()) {
            return;
        }
        this.expandedAiSlot.update(current => (current === slot ? null : slot));
    }

    protected createMealFromAiResult(result: AiInputBarResult): void {
        this.submittedAiSlot = this.expandedAiSlot();
        this.aiMealCreateRequested.emit(result);
    }
}
