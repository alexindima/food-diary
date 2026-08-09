import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiProgressRingComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-gamification-health-score-card',
    imports: [TranslatePipe, FdUiCardComponent, FdUiProgressRingComponent],
    templateUrl: './gamification-health-score-card.html',
    styleUrl: '../../gamification-page/gamification-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationHealthScoreCardComponent {
    public readonly score = input.required<number>();
}
