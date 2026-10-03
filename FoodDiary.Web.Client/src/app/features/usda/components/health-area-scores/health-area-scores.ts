import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { HealthAreaScores } from '../../../../shared/models/usda.data';
import { buildHealthAreaDisplays } from '../../lib/usda-health-score.mapper';

@Component({
    selector: 'fd-health-area-scores',
    imports: [TranslatePipe],
    templateUrl: './health-area-scores.html',
    styleUrls: ['./health-area-scores.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthAreaScoresComponent {
    public readonly scores = input<HealthAreaScores | null>(null);

    protected readonly areas = computed(() => buildHealthAreaDisplays(this.scores()));
}
