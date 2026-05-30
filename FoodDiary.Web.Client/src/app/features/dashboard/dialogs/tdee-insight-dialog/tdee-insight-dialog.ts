import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import {
    buildTdeeConfidenceKey,
    buildTdeeHintKey,
    formatTdeeWeightTrend,
    getEffectiveTdee,
    hasMeaningfulTdeeSuggestion,
} from '../../lib/tdee-insight-view.mapper';
import { TdeeInsightDialogFooterComponent } from './tdee-insight-dialog-footer/tdee-insight-dialog-footer';
import { TdeeInsightDialogHintComponent } from './tdee-insight-dialog-hint/tdee-insight-dialog-hint';
import type { TdeeInsightDialogAction, TdeeInsightDialogData, TdeeSetupItem } from './tdee-insight-dialog-lib/tdee-insight-dialog.types';
import { TdeeInsightDialogMetricsComponent } from './tdee-insight-dialog-metrics/tdee-insight-dialog-metrics';
import { TdeeInsightDialogSetupComponent } from './tdee-insight-dialog-setup/tdee-insight-dialog-setup';
import { TdeeInsightDialogSummaryComponent } from './tdee-insight-dialog-summary/tdee-insight-dialog-summary';

const MIN_FOOD_WINDOW_DAYS = 14;

@Component({
    selector: 'fd-tdee-insight-dialog',
    imports: [
        TranslateModule,
        FdUiDialogComponent,
        TdeeInsightDialogFooterComponent,
        TdeeInsightDialogHintComponent,
        TdeeInsightDialogMetricsComponent,
        TdeeInsightDialogSetupComponent,
        TdeeInsightDialogSummaryComponent,
    ],
    templateUrl: './tdee-insight-dialog.html',
    styleUrl: './tdee-insight-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightDialogComponent {
    private readonly dialogRef = inject<FdUiDialogRef<TdeeInsightDialogComponent, TdeeInsightDialogAction | undefined>>(FdUiDialogRef);
    private readonly data = inject<TdeeInsightDialogData | null>(FD_UI_DIALOG_DATA, { optional: true });

    protected readonly insight = this.data?.insight ?? null;
    protected readonly effectiveTdee = getEffectiveTdee(this.insight);
    protected readonly hasProfileBasis = (this.insight?.estimatedTdee ?? this.insight?.bmr ?? 0) > 0;
    protected readonly hasFoodWindow = (this.insight?.dataDaysUsed ?? 0) >= MIN_FOOD_WINDOW_DAYS || (this.insight?.adaptiveTdee ?? 0) > 0;
    protected readonly hasBodyTrend = this.hasBodyTrendValue();
    protected readonly weightTrendFormatted = formatTdeeWeightTrend(this.insight?.weightTrendPerWeek);
    protected readonly confidenceKey = buildTdeeConfidenceKey(this.insight, 'TDEE_DIALOG.CONFIDENCE.NONE');
    protected readonly stateKey = this.buildTdeeStateKey('STATE');
    protected readonly summaryKey = this.buildTdeeStateKey('SUMMARY');
    protected readonly showSuggestion = hasMeaningfulTdeeSuggestion(this.insight);
    protected readonly hintKey = buildTdeeHintKey(this.insight?.goalAdjustmentHint);
    protected readonly setupItems: readonly TdeeSetupItem[] = [
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

    protected close(action?: TdeeInsightDialogAction): void {
        this.dialogRef.close(action);
    }

    protected applySuggestion(): void {
        const target = this.insight?.suggestedCalorieTarget;
        if (target === null || target === undefined || target <= 0) {
            return;
        }

        this.close({ type: 'applyGoal', target });
    }

    private hasBodyTrendValue(): boolean {
        return (this.insight?.adaptiveTdee ?? 0) > 0 || this.insight?.weightTrendPerWeek !== null;
    }

    private buildTdeeStateKey(section: 'STATE' | 'SUMMARY'): string {
        if (this.effectiveTdee === null || this.effectiveTdee <= 0) {
            return `TDEE_DIALOG.${section}.EMPTY`;
        }

        return (this.insight?.adaptiveTdee ?? 0) > 0 ? `TDEE_DIALOG.${section}.ADAPTIVE` : `TDEE_DIALOG.${section}.ESTIMATED`;
    }
}
