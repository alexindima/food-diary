import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { type HealthAreaScores } from '../../models/usda.data';

interface HealthAreaDisplay {
    key: string;
    labelKey: string;
    icon: string;
    score: number;
    grade: string;
    gradeKey: string;
    strokeDasharray: string;
}

@Component({
    selector: 'fd-health-area-scores',
    standalone: true,
    imports: [CommonModule, TranslatePipe],
    templateUrl: './health-area-scores.component.html',
    styleUrls: ['./health-area-scores.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthAreaScoresComponent {
    public readonly scores = input<HealthAreaScores | null>(null);

    public readonly areas = computed<HealthAreaDisplay[]>(() => {
        const s = this.scores();
        if (!s) {
            return [];
        }
        return [
            { key: 'HEART', icon: '\u2764\uFE0F', score: s.heart.score, grade: s.heart.grade },
            { key: 'BONE', icon: '\uD83E\uDDB4', score: s.bone.score, grade: s.bone.grade },
            { key: 'IMMUNE', icon: '\uD83D\uDEE1\uFE0F', score: s.immune.score, grade: s.immune.grade },
            { key: 'ENERGY', icon: '\u26A1', score: s.energy.score, grade: s.energy.grade },
            { key: 'ANTIOXIDANT', icon: '\uD83E\uDDEC', score: s.antioxidant.score, grade: s.antioxidant.grade },
        ].map(area => ({
            ...area,
            labelKey: `HEALTH_SCORES.${area.key}`,
            gradeKey: `HEALTH_SCORES.GRADE_${area.grade.toUpperCase()}`,
            strokeDasharray: `${area.score}, 100`,
        }));
    });
}
