import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { DashboardWidgetFrameComponent } from '../../../dashboard/components/dashboard-widget-frame/dashboard-widget-frame.component';
import { type FastingOccurrenceKind } from '../../models/fasting.data';

@Component({
    selector: 'fd-fasting-timer-card',
    standalone: true,
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent, DashboardWidgetFrameComponent],
    templateUrl: './fasting-timer-card.component.html',
    styleUrl: './fasting-timer-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardComponent {
    protected readonly Math = Math;
    public readonly layout = input<'stacked' | 'summary' | 'setup' | 'pageSummary'>('stacked');
    public readonly isActive = input<boolean>(false);
    public readonly isOvertime = input<boolean>(false);
    public readonly currentSessionCompleted = input<boolean>(false);
    public readonly progressPercent = input<number>(0);
    public readonly elapsedFormatted = input<string>('00:00:00');
    public readonly remainingFormatted = input<string>('00:00:00');
    public readonly remainingLabelKey = input<string>('FASTING.REMAINING');
    public readonly labelKey = input<string>('FASTING.WIDGET_LABEL');
    public readonly stateLabel = input<string | null>(null);
    public readonly occurrenceKind = input<FastingOccurrenceKind | null>(null);
    public readonly detailLabel = input<string | null>(null);
    public readonly metaLabel = input<string | null>(null);
    public readonly ringColor = input<string | null>(null);
    public readonly glowColor = input<string | null>(null);
    public readonly stageTitleKey = input<string | null>(null);
    public readonly stageDescriptionKey = input<string | null>(null);
    public readonly stageIndex = input<number | null>(null);
    public readonly totalStages = input<number>(4);
    public readonly nextStageTitleKey = input<string | null>(null);
    public readonly nextStageFormatted = input<string | null>(null);
    public readonly showGlow = input<boolean>(true);

    public getProgressStrokeColor(): string {
        if (this.isOvertime()) {
            return 'var(--fd-color-green-500)';
        }

        if (this.isEatingPhase()) {
            return 'var(--fd-color-green-500)';
        }

        return this.ringColor() ?? 'var(--fd-color-purple-500)';
    }

    public getRingGlow(): string | null {
        if (!this.showGlow()) {
            return null;
        }

        if (this.isOvertime()) {
            return 'var(--fd-shadow-fasting-overtime-ring)';
        }

        const glowColor = this.glowColor();
        if (!glowColor) {
            return null;
        }

        return `0 0 0 var(--fd-size-fasting-ring-glow-spread) ${glowColor}, 0 var(--fd-size-fasting-ring-glow-offset-y) var(--fd-size-fasting-ring-glow-blur) ${glowColor}`;
    }

    public shouldShowStageProgress(): boolean {
        return !this.isEatingPhase() && this.stageTitleKey() !== null && this.stageIndex() !== null && this.totalStages() > 0;
    }

    public shouldShowStageDescriptionFallback(): boolean {
        return !this.isEatingPhase() && !this.shouldShowStageProgress() && this.stageDescriptionKey() !== null;
    }

    private isEatingPhase(): boolean {
        const occurrenceKind = this.occurrenceKind();
        return occurrenceKind === 'EatDay' || occurrenceKind === 'EatingWindow';
    }
}
