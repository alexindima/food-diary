import { Directive, input, output } from '@angular/core';

/** Chart-grid surfaces cannot use the UI kit button's extra layout wrapper. */
@Directive({
    selector: '[fdDaySummaryMetric]',
    host: {
        '[attr.role]': 'fdDaySummaryMetric() ? "button" : null',
        '[attr.tabindex]': 'fdDaySummaryMetric() ? 0 : null',
        '[attr.aria-haspopup]': 'fdDaySummaryMetric() ? "dialog" : null',
        '[class.day-summary__metric-action]': 'fdDaySummaryMetric()',
        '(click)': 'activate()',
        '(keydown.enter)': 'activateFromKeyboard($event)',
        '(keydown.space)': 'activateFromKeyboard($event)',
    },
})
export class DaySummaryMetricDirective {
    public readonly fdDaySummaryMetric = input(false);
    public readonly metricActivated = output();

    protected activate(): void {
        if (this.fdDaySummaryMetric()) {
            this.metricActivated.emit();
        }
    }

    protected activateFromKeyboard(event: Event): void {
        if (this.fdDaySummaryMetric()) {
            event.preventDefault();
            if (event instanceof KeyboardEvent && !event.repeat) {
                this.metricActivated.emit();
            }
        }
    }
}
