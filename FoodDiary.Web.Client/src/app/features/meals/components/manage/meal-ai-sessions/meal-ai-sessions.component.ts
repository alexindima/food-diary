import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import type { ConsumptionAiItemManageDto, ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import { formatMealAiAmount, formatMealAiName, formatMealManageMacro, getAiSessionTotals } from '../meal-manage-view.utils';

@Component({
    selector: 'fd-meal-ai-sessions',
    templateUrl: './meal-ai-sessions.component.html',
    styleUrls: ['../meal-manage-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent],
})
export class MealAiSessionsComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly aiSessions = input.required<ConsumptionAiSessionManageDto[]>();

    public readonly editSession = output<number>();
    public readonly deleteSession = output<number>();

    public readonly aiPreviewMaxItems = 2;
    public readonly expandedAiSessions = signal<Set<number>>(new Set());
    public readonly aiSessionRows = computed<AiSessionRowViewModel[]>(() => {
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

    public getAiSessionLabel(index: number): string {
        return this.translateService.instant('CONSUMPTION_MANAGE.ITEMS_AI_PHOTO_LABEL', { index: index + 1 });
    }

    public toggleAiSessionExpanded(index: number): void {
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

    private visibleAiItems(items: ConsumptionAiItemManageDto[], maxVisible: number): ConsumptionAiItemManageDto[] {
        return items.slice(0, Math.max(0, maxVisible));
    }

    private getHiddenAiItemsCount(items: ConsumptionAiItemManageDto[], maxVisible: number): number {
        return Math.max(0, items.length - Math.max(0, maxVisible));
    }

    public onEditSession(index: number): void {
        this.editSession.emit(index);
    }

    public onDeleteSession(index: number): void {
        this.deleteSession.emit(index);
    }
}

type AiSessionRowViewModel = {
    session: ConsumptionAiSessionManageDto;
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
