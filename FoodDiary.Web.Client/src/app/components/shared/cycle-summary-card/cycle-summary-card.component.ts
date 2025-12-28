import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';
import { CyclePredictions } from '../../../types/cycle.data';
import { FdCardHoverDirective } from '../../../directives/card-hover.directive';

@Component({
    selector: 'fd-cycle-summary-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NoticeBannerComponent, FdCardHoverDirective],
    templateUrl: './cycle-summary-card.component.html',
    styleUrl: './cycle-summary-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSummaryCardComponent {
    public readonly title = input<string>('CYCLE_CARD.TITLE');
    public readonly startDate = input<string | null>(null);
    public readonly predictions = input<CyclePredictions | null>(null);
    public readonly referenceDate = input<Date | string | null>(null);
    public readonly isLoading = input<boolean>(false);
    public readonly setupAction = output<void>();

    private readonly normalizedStart = computed(() => this.normalizeDate(this.startDate()));
    private readonly normalizedReference = computed(() => this.normalizeDate(this.referenceDate()));

    public readonly cycleDay = computed(() => {
        const start = this.normalizedStart();
        const reference = this.normalizedReference();
        if (!start || !reference) {
            return null;
        }
        const diff = Math.floor((reference.getTime() - start.getTime()) / 86400000);
        return Math.max(1, diff + 1);
    });

    public readonly statusKey = computed(() => {
        const reference = this.normalizedReference();
        if (!reference) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationDate ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStart ?? null);

        if (ovulation) {
            const days = this.daysBetween(reference, ovulation);
            if (days >= 0) {
                return 'CYCLE_CARD.OVULATION_IN';
            }
        }

        if (nextPeriod) {
            const days = this.daysBetween(reference, nextPeriod);
            if (days <= 0) {
                return 'CYCLE_CARD.NEXT_PERIOD_TODAY';
            }
            return 'CYCLE_CARD.NEXT_PERIOD_IN';
        }

        return null;
    });

    public readonly statusDays = computed(() => {
        const reference = this.normalizedReference();
        if (!reference) {
            return null;
        }

        const predictions = this.predictions();
        const ovulation = this.normalizeDate(predictions?.ovulationDate ?? null);
        const nextPeriod = this.normalizeDate(predictions?.nextPeriodStart ?? null);

        if (ovulation) {
            const days = this.daysBetween(reference, ovulation);
            if (days >= 0) {
                return days;
            }
        }

        if (nextPeriod) {
            const days = this.daysBetween(reference, nextPeriod);
            if (days > 0) {
                return days;
            }
        }

        return null;
    });

    public readonly hasCycle = computed(() => !!this.normalizedStart());

    public onSetup(): void {
        this.setupAction.emit();
    }

    private normalizeDate(value: Date | string | null | undefined): Date | null {
        if (!value) {
            return null;
        }
        const date = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private daysBetween(from: Date, to: Date): number {
        return Math.round((to.getTime() - from.getTime()) / 86400000);
    }
}
