import { describe, expect, it } from 'vitest';

import { Gender } from '../../../../../shared/models/user.data';
import { calculateProfileCompleteness } from './user-profile-completeness.mapper';

const THREE_QUARTERS_COMPLETE = 75;
const FULLY_COMPLETE = 100;

describe('calculateProfileCompleteness', () => {
    it('returns zero when calculation profile fields are empty', () => {
        expect(calculateProfileCompleteness({ birthDate: null, gender: null, heightCm: null, activityLevel: null })).toBe(0);
    });

    it('weights each calculation profile field equally', () => {
        expect(
            calculateProfileCompleteness({
                birthDate: null,
                gender: Gender.Male,
                heightCm: 175,
                activityLevel: 'MODERATE',
            }),
        ).toBe(THREE_QUARTERS_COMPLETE);
    });

    it('returns one hundred when all calculation profile fields are filled', () => {
        expect(
            calculateProfileCompleteness({
                birthDate: '1990-01-01',
                gender: Gender.Female,
                heightCm: 168,
                activityLevel: 'LIGHT',
            }),
        ).toBe(FULLY_COMPLETE);
    });
});
