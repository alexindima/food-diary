import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { HealthScoreRing } from '../../gamification-page-lib/gamification-page.models';
import { calculateHealthScoreRing } from '../../gamification-page-lib/gamification-page-view.mapper';

@Component({
    selector: 'fd-gamification-health-score-card',
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './gamification-health-score-card.component.html',
    styleUrl: '../../gamification-page/gamification-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamificationHealthScoreCardComponent {
    public readonly score = input.required<number>();

    protected readonly ring = computed<HealthScoreRing>(() => calculateHealthScoreRing(this.score()));
}
