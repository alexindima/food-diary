import type { HealthAreaScores } from '../models/usda.data';
import type { HealthAreaDisplay } from './usda-view.types';

const MAX_HEALTH_AREA_SCORE = 100;

export function buildHealthAreaDisplays(scores: HealthAreaScores | null): HealthAreaDisplay[] {
    if (scores === null) {
        return [];
    }

    return [
        { key: 'HEART', icon: '\u2764\uFE0F', score: scores.heart.score, grade: scores.heart.grade },
        { key: 'BONE', icon: '\uD83E\uDDB4', score: scores.bone.score, grade: scores.bone.grade },
        { key: 'IMMUNE', icon: '\uD83D\uDEE1\uFE0F', score: scores.immune.score, grade: scores.immune.grade },
        { key: 'ENERGY', icon: '\u26A1', score: scores.energy.score, grade: scores.energy.grade },
        { key: 'ANTIOXIDANT', icon: '\uD83E\uDDEC', score: scores.antioxidant.score, grade: scores.antioxidant.grade },
    ].map(area => ({
        ...area,
        labelKey: `HEALTH_SCORES.${area.key}`,
        gradeKey: `HEALTH_SCORES.GRADE_${area.grade.toUpperCase()}`,
        gradeClass: `area-card--${area.grade}`,
        strokeDasharray: `${area.score}, ${MAX_HEALTH_AREA_SCORE}`,
    }));
}
