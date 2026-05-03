import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon.component';

import { type ConsumptionAiItemManageDto, type ConsumptionAiSessionManageDto } from '../../../models/meal.data';
import { type NutritionTotals } from '../base-meal-manage.types';

@Component({
    selector: 'fd-meal-ai-sessions',
    templateUrl: './meal-ai-sessions.component.html',
    styleUrls: ['../base-meal-manage.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiHintDirective, FdUiCardComponent, FdUiButtonComponent, FdUiIconComponent],
})
export class MealAiSessionsComponent {
    private readonly translateService = inject(TranslateService);

    public readonly aiSessions = input.required<ConsumptionAiSessionManageDto[]>();
    public readonly aiQuotaExceeded = input<boolean>(false);

    public readonly addFromPhoto = output<void>();
    public readonly editSession = output<number>();
    public readonly deleteSession = output<number>();

    public readonly aiPreviewMaxItems = 2;
    public readonly expandedAiSessions = signal<Set<number>>(new Set());

    public formatAiAmount(amount: number, unit: string): string {
        const normalized = unit?.trim().toLowerCase();
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

    public formatAiName(name?: string | null): string {
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

    public formatAiMacro(value: number, unitKey: string): string {
        const locale = this.translateService.currentLang || this.translateService.defaultLang || 'en';
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

    public getAiSessionTotals(session: ConsumptionAiSessionManageDto): NutritionTotals {
        return session.items.reduce(
            (totals, item) => ({
                calories: totals.calories + (item.calories ?? 0),
                proteins: totals.proteins + (item.proteins ?? 0),
                fats: totals.fats + (item.fats ?? 0),
                carbs: totals.carbs + (item.carbs ?? 0),
                fiber: totals.fiber + (item.fiber ?? 0),
                alcohol: totals.alcohol + (item.alcohol ?? 0),
            }),
            { calories: 0, proteins: 0, fats: 0, carbs: 0, fiber: 0, alcohol: 0 },
        );
    }

    public isAiSessionExpanded(index: number): boolean {
        return this.expandedAiSessions().has(index);
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

    public visibleAiItems(items: ConsumptionAiItemManageDto[], maxVisible: number): ConsumptionAiItemManageDto[] {
        return items.slice(0, Math.max(0, maxVisible));
    }

    public getHiddenAiItemsCount(items: ConsumptionAiItemManageDto[], maxVisible: number): number {
        return Math.max(0, items.length - Math.max(0, maxVisible));
    }

    public onAddFromPhoto(): void {
        this.addFromPhoto.emit();
    }

    public onEditSession(index: number): void {
        this.editSession.emit(index);
    }

    public onDeleteSession(index: number): void {
        this.deleteSession.emit(index);
    }
}
