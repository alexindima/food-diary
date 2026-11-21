import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';

import type { Meal, MealAiItemManageDto, MealAiSessionManageDto } from '../../../../../shared/models/meal.data';
import { formatMealAiAmount, formatMealAiName, formatMealManageMacro, getAiSessionTotals } from '../meal-manage-lib/meal-manage-view.utils';

export type MealAiSessionRemoval = {
    session: MealAiSessionManageDto;
    onRemoved: (index: number) => void;
};

@Component({
    selector: 'fd-meal-ai-sessions',
    templateUrl: './meal-ai-sessions.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent],
})
export class MealAiSessionsComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly aiSessions = input.required<MealAiSessionManageDto[]>();
    public readonly mealId = input<Meal['id'] | null>(null);

    public readonly editSession = output<MealAiSessionManageDto>();
    public readonly deleteSession = output<MealAiSessionRemoval>();

    protected readonly aiPreviewMaxItems = 2;
    private readonly expandedAiSessions = linkedSignal({
        source: this.mealId,
        computation: () => new Set<number>(),
    });
    protected readonly aiSessionRows = computed<AiSessionRowViewModel[]>(() => {
        const expandedAiSessions = this.expandedAiSessions();
        this.activeLang();

        return this.aiSessions().map((session, index) => {
            const totals = getAiSessionTotals(session);
            const isExpanded = expandedAiSessions.has(index);
            const visibleItems = isExpanded ? session.items : this.visibleAiItems(session.items, this.aiPreviewMaxItems);

            return {
                session,
                index,
                itemCount: session.items.length,
                isExpanded,
                hiddenItemsCount: this.getHiddenAiItemsCount(session.items, this.aiPreviewMaxItems),
                visibleItems: visibleItems.map(item => ({
                    nameLabel: formatMealAiName(item.nameLocal ?? item.nameEn),
                    amountLabel: formatMealAiAmount(item.amount, item.unit, this.translateService),
                })),
                caloriesLabel: formatMealManageMacro(totals.calories, 'GENERAL.UNITS.KCAL', this.translateService),
                proteinsLabel: formatMealManageMacro(totals.proteins, 'GENERAL.UNITS.G', this.translateService),
                fatsLabel: formatMealManageMacro(totals.fats, 'GENERAL.UNITS.G', this.translateService),
                carbsLabel: formatMealManageMacro(totals.carbs, 'GENERAL.UNITS.G', this.translateService),
            };
        });
    });

    private readonly activeLang = signal(this.translateService.getCurrentLang());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    protected toggleAiSessionExpanded(index: number): void {
        this.expandedAiSessions.update(current => {
            const next = new Set(current);
            if (next.has(index)) {
                next.delete(index);
            } else {
                next.add(index);
            }
            return next;
        });
    }

    private visibleAiItems(items: MealAiItemManageDto[], maxVisible: number): MealAiItemManageDto[] {
        return items.slice(0, Math.max(0, maxVisible));
    }

    private getHiddenAiItemsCount(items: MealAiItemManageDto[], maxVisible: number): number {
        return Math.max(0, items.length - Math.max(0, maxVisible));
    }

    protected onDeleteSession(session: MealAiSessionManageDto): void {
        this.deleteSession.emit({
            session,
            onRemoved: index => {
                this.reindexExpandedAfterRemoval(index);
            },
        });
    }

    private reindexExpandedAfterRemoval(index: number): void {
        this.expandedAiSessions.update(expanded => {
            const remaining = new Set<number>();
            for (const expandedIndex of expanded) {
                if (expandedIndex !== index) {
                    remaining.add(expandedIndex > index ? expandedIndex - 1 : expandedIndex);
                }
            }
            return remaining;
        });
    }
}

type AiSessionRowViewModel = {
    session: MealAiSessionManageDto;
    index: number;
    itemCount: number;
    isExpanded: boolean;
    hiddenItemsCount: number;
    visibleItems: AiSessionItemViewModel[];
    caloriesLabel: string;
    proteinsLabel: string;
    fatsLabel: string;
    carbsLabel: string;
};

type AiSessionItemViewModel = {
    nameLabel: string;
    amountLabel: string;
};
