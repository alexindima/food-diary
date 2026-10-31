import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type FdUiChartLegendItem = {
    label: string;
    color: string;
};

@Component({
    selector: 'fd-ui-chart-legend',
    templateUrl: './fd-ui-chart-legend.html',
    styleUrl: './fd-ui-chart-legend.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-ui-chart-legend--hidden]': '!visible()',
        '[attr.aria-hidden]': '!visible() ? "true" : null',
    },
})
export class FdUiChartLegendComponent {
    public readonly items = input<readonly FdUiChartLegendItem[]>([]);
    // Keep the same layout space while switching to a metric without a legend.
    public readonly visible = input(true);
}
