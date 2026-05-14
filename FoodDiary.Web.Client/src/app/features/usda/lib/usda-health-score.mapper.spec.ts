import { describe, expect, it } from 'vitest';

import { buildHealthAreaDisplays } from './usda-health-score.mapper';

const HEART_SCORE = 82;
const BONE_SCORE = 71;
const IMMUNE_SCORE = 64;
const ENERGY_SCORE = 55;
const ANTIOXIDANT_SCORE = 47;

describe('USDA health score mapper', () => {
    it('returns empty displays when scores are missing', () => {
        expect(buildHealthAreaDisplays(null)).toEqual([]);
    });

    it('builds display metadata for every health area', () => {
        const displays = buildHealthAreaDisplays({
            heart: { score: HEART_SCORE, grade: 'excellent' },
            bone: { score: BONE_SCORE, grade: 'good' },
            immune: { score: IMMUNE_SCORE, grade: 'fair' },
            energy: { score: ENERGY_SCORE, grade: 'low' },
            antioxidant: { score: ANTIOXIDANT_SCORE, grade: 'unknown' },
        });

        expect(displays.map(display => display.key)).toEqual(['HEART', 'BONE', 'IMMUNE', 'ENERGY', 'ANTIOXIDANT']);
        expect(displays[0]).toEqual(
            expect.objectContaining({
                labelKey: 'HEALTH_SCORES.HEART',
                gradeKey: 'HEALTH_SCORES.GRADE_EXCELLENT',
                gradeClass: 'area-card--excellent',
                strokeDasharray: `${HEART_SCORE}, 100`,
            }),
        );
    });
});
