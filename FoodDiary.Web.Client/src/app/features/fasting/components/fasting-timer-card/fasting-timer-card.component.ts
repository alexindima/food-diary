import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-fasting-timer-card',
    standalone: true,
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './fasting-timer-card.component.html',
    styleUrl: './fasting-timer-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardComponent {
    protected readonly Math = Math;
    public readonly layout = input<'stacked' | 'summary'>('stacked');
    public readonly isActive = input<boolean>(false);
    public readonly isOvertime = input<boolean>(false);
    public readonly currentSessionCompleted = input<boolean>(false);
    public readonly progressPercent = input<number>(0);
    public readonly elapsedFormatted = input<string>('00:00:00');
    public readonly remainingFormatted = input<string>('00:00:00');
    public readonly remainingLabelKey = input<string>('FASTING.REMAINING');
    public readonly labelKey = input<string>('FASTING.WIDGET_LABEL');
    public readonly stateLabel = input<string | null>(null);
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
            return '#22c55e';
        }

        return this.ringColor() ?? '#7c3aed';
    }

    public getRingGlow(): string | null {
        if (!this.showGlow()) {
            return null;
        }

        if (this.isOvertime()) {
            return '0 0 0 10px rgba(34, 197, 94, 0.14), 0 18px 36px rgba(34, 197, 94, 0.12)';
        }

        const glowColor = this.glowColor();
        if (!glowColor) {
            return null;
        }

        return `0 0 0 10px ${glowColor}, 0 18px 36px ${glowColor}`;
    }

    public shouldShowStageProgress(): boolean {
        return this.stageTitleKey() !== null && this.stageIndex() !== null && this.totalStages() > 0;
    }
}
