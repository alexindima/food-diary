import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { TdeeInsight } from '../../models/tdee-insight.data';

const MIN_FOOD_WINDOW_DAYS = 14;
const SUGGESTION_DIFF_THRESHOLD = 50;
const WEIGHT_TREND_FRACTION_DIGITS = 2;

export interface TdeeInsightDialogData {
    insight: TdeeInsight | null;
}

export type TdeeInsightDialogAction =
    | { type: 'profile' }
    | { type: 'meal' }
    | { type: 'weight' }
    | { type: 'goals' }
    | { type: 'applyGoal'; target: number };

interface TdeeSetupItem {
    readonly key: string;
    readonly icon: string;
    readonly complete: boolean;
    readonly titleKey: string;
    readonly textKey: string;
}

@Component({
    selector: 'fd-tdee-insight-dialog',
    standalone: true,
    imports: [CommonModule, TranslateModule, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './tdee-insight-dialog.component.html',
    styleUrl: './tdee-insight-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<TdeeInsightDialogComponent, TdeeInsightDialogAction | undefined>>(FdUiDialogRef);
    private readonly data = inject<TdeeInsightDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });

    public readonly insight = this.data?.insight ?? null;
    public readonly effectiveTdee = this.insight?.adaptiveTdee ?? this.insight?.estimatedTdee ?? null;
    public readonly hasProfileBasis = (this.insight?.estimatedTdee ?? this.insight?.bmr ?? 0) > 0;
    public readonly hasFoodWindow = (this.insight?.dataDaysUsed ?? 0) >= MIN_FOOD_WINDOW_DAYS || (this.insight?.adaptiveTdee ?? 0) > 0;
    public readonly hasBodyTrend = this.hasBodyTrendValue();
    public readonly weightTrendFormatted = this.formatWeightTrend(this.insight?.weightTrendPerWeek);
    public readonly confidenceKey = this.buildConfidenceKey();
    public readonly stateKey = this.buildTdeeStateKey('STATE');
    public readonly summaryKey = this.buildTdeeStateKey('SUMMARY');
    public readonly showSuggestion = this.hasMeaningfulSuggestion();
    public readonly hintKey = this.buildHintKey();
    public readonly setupItems: readonly TdeeSetupItem[] = [
        {
            key: 'profile',
            icon: 'person',
            complete: this.hasProfileBasis,
            titleKey: 'TDEE_DIALOG.SETUP.PROFILE_TITLE',
            textKey: 'TDEE_DIALOG.SETUP.PROFILE_TEXT',
        },
        {
            key: 'meals',
            icon: 'restaurant',
            complete: this.hasFoodWindow,
            titleKey: 'TDEE_DIALOG.SETUP.MEALS_TITLE',
            textKey: 'TDEE_DIALOG.SETUP.MEALS_TEXT',
        },
        {
            key: 'weight',
            icon: 'monitor_weight',
            complete: this.hasBodyTrend,
            titleKey: 'TDEE_DIALOG.SETUP.WEIGHT_TITLE',
            textKey: 'TDEE_DIALOG.SETUP.WEIGHT_TEXT',
        },
    ];

    public close(action?: TdeeInsightDialogAction): void {
        this.dialogRef.close(action);
    }

    public applySuggestion(): void {
        const target = this.insight?.suggestedCalorieTarget;
        if (target === null || target === undefined || target <= 0) {
            return;
        }

        this.close({ type: 'applyGoal', target });
    }

    private formatWeightTrend(value: number | null | undefined): string | null {
        if (value === null || value === undefined) {
            return null;
        }

        const sign = value > 0 ? '+' : '';
        return `${sign}${value.toFixed(WEIGHT_TREND_FRACTION_DIGITS)}`;
    }

    private buildHintKey(): string | null {
        const hint = this.insight?.goalAdjustmentHint ?? '';
        return hint.length > 0 ? `TDEE_CARD.HINTS.${hint.replace('hint.', '').toUpperCase()}` : null;
    }

    private hasBodyTrendValue(): boolean {
        return (this.insight?.adaptiveTdee ?? 0) > 0 || this.insight?.weightTrendPerWeek !== null;
    }

    private buildConfidenceKey(): string {
        return this.insight !== null && this.insight.confidence !== 'none'
            ? `TDEE_CARD.CONFIDENCE.${this.insight.confidence.toUpperCase()}`
            : 'TDEE_DIALOG.CONFIDENCE.NONE';
    }

    private buildTdeeStateKey(section: 'STATE' | 'SUMMARY'): string {
        if (this.effectiveTdee === null || this.effectiveTdee <= 0) {
            return `TDEE_DIALOG.${section}.EMPTY`;
        }

        return (this.insight?.adaptiveTdee ?? 0) > 0 ? `TDEE_DIALOG.${section}.ADAPTIVE` : `TDEE_DIALOG.${section}.ESTIMATED`;
    }

    private hasMeaningfulSuggestion(): boolean {
        const suggested = this.insight?.suggestedCalorieTarget ?? 0;
        const current = this.insight?.currentCalorieTarget ?? 0;
        return suggested > 0 && current > 0 && Math.abs(suggested - current) > SUGGESTION_DIFF_THRESHOLD;
    }
}
