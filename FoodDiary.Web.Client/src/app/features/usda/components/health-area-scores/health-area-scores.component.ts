import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { buildHealthAreaDisplays } from '../../lib/usda-health-score.mapper';
import type { HealthAreaScores } from '../../models/usda.data';

@Component({
    selector: 'fd-health-area-scores',
    imports: [TranslatePipe],
    templateUrl: './health-area-scores.component.html',
    styleUrls: ['./health-area-scores.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthAreaScoresComponent {
    public readonly scores = input<HealthAreaScores | null>(null);

    public readonly areas = computed(() => buildHealthAreaDisplays(this.scores()));
}
