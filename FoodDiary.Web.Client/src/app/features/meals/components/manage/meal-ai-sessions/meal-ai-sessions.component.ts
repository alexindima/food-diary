import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import type { ConsumptionAiItemManageDto, ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import type { NutritionTotals } from '../base-meal-manage.types';

@Component({
    selector: 'fd-meal-ai-sessions',
    templateUrl: './meal-ai-sessions.component.html',
    styleUrls: ['../base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgOptimizedImage, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiIconComponent],
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
            const totals = this.getAiSessionTotals(session);
            const isExpanded = expandedAiSessions.has(index);
            const visibleItems = isExpanded ? session.items : this.visibleAiItems(session.items, this.aiPreviewMaxItems);

            return {
                session,
                index,
                itemCount: session.items.length,
                isExpanded,
                hiddenItemsCount: this.getHiddenAiItemsCount(session.items, this.aiPreviewMaxItems),
                visibleItems: visibleItems.map(item => ({
                    nameLabel: this.formatAiName(item.nameLocal ?? item.nameEn),
                    amountLabel: this.formatAiAmount(item.amount, item.unit),
                })),
                caloriesLabel: this.formatAiMacro(totals.calories, 'GENERAL.UNITS.KCAL'),
                proteinsLabel: this.formatAiMacro(totals.proteins, 'GENERAL.UNITS.G'),
                fatsLabel: this.formatAiMacro(totals.fats, 'GENERAL.UNITS.G'),
                carbsLabel: this.formatAiMacro(totals.carbs, 'GENERAL.UNITS.G'),
            };
        });
    });

    private readonly activeLang = signal(this.translateService.getCurrentLang());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    private formatAiAmount(amount: number, unit: string): string {
        const normalized = unit.trim().toLowerCase();
        const unitMap: Record<string, string> = {
            g: 'GENERAL.UNITS.G',
            gram: 'GENERAL.UNITS.G',
            grams: 'GENERAL.UNITS.G',
            gr: 'GENERAL.UNITS.G',
            ml: 'GENERAL.UNITS.ML',
            l: 'GENERAL.UNITS.ML',
            pcs: 'GENERAL.UNITS.PCS',
            pc: 'GENERAL.UNITS.PCS',
            piece: 'GENERAL.UNITS.PCS',
            pieces: 'GENERAL.UNITS.PCS',
            kcal: 'GENERAL.UNITS.KCAL',
        };

        const unitKey = normalized ? unitMap[normalized] : null;
        const unitLabel = unitKey ? this.translateService.instant(unitKey) : unit;
        return unitLabel ? `${amount} ${unitLabel}`.trim() : `${amount}`.trim();
    }

    private formatAiName(name?: string | null): string {
        if (!name) {
            return '';
        }

        const trimmed = name.trim();
        if (!trimmed) {
            return '';
        }

        const [first, ...rest] = trimmed;
        return `${first.toLocaleUpperCase()}${rest.join('')}`;
    }

    private formatAiMacro(value: number, unitKey: string): string {
        const locale = (this.translateService.getCurrentLang() || this.translateService.getFallbackLang()) ?? 'en';
        const hasFraction = Math.abs(value % 1) > 0.01;
        const formatter = new Intl.NumberFormat(locale, {
            maximumFractionDigits: hasFraction ? 1 : 0,
            minimumFractionDigits: hasFraction ? 1 : 0,
        });
        const unitLabel = this.translateService.instant(unitKey);
        return `${formatter.format(value)} ${unitLabel}`.trim();
    }

    public getAiSessionLabel(index: number): string {
        return this.translateService.instant('CONSUMPTION_MANAGE.ITEMS_AI_PHOTO_LABEL', { index: index + 1 });
    }

    private getAiSessionTotals(session: ConsumptionAiSessionManageDto): NutritionTotals {
        return session.items.reduce(
            (totals, item) => ({
                calories: totals.calories + item.calories,
                proteins: totals.proteins + item.proteins,
                fats: totals.fats + item.fats,
                carbs: totals.carbs + item.carbs,
                fiber: totals.fiber + item.fiber,
                alcohol: totals.alcohol + item.alcohol,
            }),
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );
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

interface AiSessionRowViewModel {
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
}

interface AiSessionItemViewModel {
    nameLabel: string;
    amountLabel: string;
}
