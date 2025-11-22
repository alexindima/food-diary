import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-activity-card',
    standalone: true,
    imports: [FdUiCardComponent, TranslatePipe],
    templateUrl: './activity-card.component.html',
    styleUrls: ['./activity-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityCardComponent {
    public readonly burnedCalories = input<number>(215);
    public readonly steps = input<number>(5420);
}
