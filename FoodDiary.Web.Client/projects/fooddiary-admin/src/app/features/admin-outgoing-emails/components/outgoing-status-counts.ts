import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-outgoing-status-counts',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './outgoing-status-counts.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OutgoingStatusCountsComponent {
    public readonly counts = input<Record<string, number> | null>(null);
    public readonly selected = output<string>();
    protected readonly entries = computed(() => Object.entries(this.counts() ?? {}).map(([status, count]) => ({ status, count })));
}
