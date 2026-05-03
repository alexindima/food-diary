import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import { type TdeeInsight } from '../../models/tdee-insight.data';

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
    public readonly hasProfileBasis = !!(this.insight?.estimatedTdee || this.insight?.bmr);
    public readonly hasFoodWindow = (this.insight?.dataDaysUsed ?? 0) >= 14 || !!this.insight?.adaptiveTdee;
    public readonly hasBodyTrend =
        !!this.insight?.adaptiveTdee || (this.insight?.weightTrendPerWeek !== null && this.insight?.weightTrendPerWeek !== undefined);
    public readonly weightTrendFormatted = this.formatWeightTrend(this.insight?.weightTrendPerWeek);
    public readonly confidenceKey =
        this.insight && this.insight.confidence !== 'none'
            ? `TDEE_CARD.CONFIDENCE.${this.insight.confidence.toUpperCase()}`
            : 'TDEE_DIALOG.CONFIDENCE.NONE';
    public readonly stateKey = !this.effectiveTdee
        ? 'TDEE_DIALOG.STATE.EMPTY'
        : this.insight?.adaptiveTdee
          ? 'TDEE_DIALOG.STATE.ADAPTIVE'
          : 'TDEE_DIALOG.STATE.ESTIMATED';
    public readonly summaryKey = !this.effectiveTdee
        ? 'TDEE_DIALOG.SUMMARY.EMPTY'
        : this.insight?.adaptiveTdee
          ? 'TDEE_DIALOG.SUMMARY.ADAPTIVE'
          : 'TDEE_DIALOG.SUMMARY.ESTIMATED';
    public readonly showSuggestion =
        !!this.insight?.suggestedCalorieTarget &&
        !!this.insight.currentCalorieTarget &&
        Math.abs(this.insight.suggestedCalorieTarget - this.insight.currentCalorieTarget) > 50;
    public readonly hintKey = this.insight?.goalAdjustmentHint
        ? `TDEE_CARD.HINTS.${this.insight.goalAdjustmentHint.replace('hint.', '').toUpperCase()}`
        : null;
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
        if (!target) {
            return;
        }

        this.close({ type: 'applyGoal', target });
    }

    private formatWeightTrend(value: number | null | undefined): string | null {
        if (value === null || value === undefined) {
            return null;
        }

        const sign = value > 0 ? '+' : '';
        return `${sign}${value.toFixed(2)}`;
    }
}
