import { describe, expect, it } from 'vitest';

import {
    centimetersToImperialHeight,
    centimetersToInches,
    imperialHeightToCentimeters,
    inchesToCentimeters,
    kilogramsToPounds,
    poundsToKilograms,
} from './measurement-system.service';

const WEIGHT_KG = 72.5;
const WEIGHT_LB = 159.8;
const ROUND_TRIP_WEIGHT_KG = 72.48;
const WAIST_CM = 81.5;
const WAIST_IN = 32.1;
const ROUND_TRIP_WAIST_CM = 81.53;
const HEIGHT_CM = 180;
const HEIGHT_FT = 5;
const HEIGHT_IN = 11;
const ROUND_TRIP_HEIGHT_CM = 180.3;

describe('measurement conversions', () => {
    it('converts kilograms and pounds at the UI boundary', () => {
        expect(kilogramsToPounds(WEIGHT_KG)).toBe(WEIGHT_LB);
        expect(poundsToKilograms(WEIGHT_LB)).toBeCloseTo(ROUND_TRIP_WEIGHT_KG, 2);
    });

    it('converts centimeters and inches at the UI boundary', () => {
        expect(centimetersToInches(WAIST_CM)).toBe(WAIST_IN);
        expect(inchesToCentimeters(WAIST_IN)).toBeCloseTo(ROUND_TRIP_WAIST_CM, 2);
    });

    it('converts height between centimeters and feet with inches', () => {
        expect(centimetersToImperialHeight(HEIGHT_CM)).toEqual({ feet: HEIGHT_FT, inches: HEIGHT_IN });
        expect(imperialHeightToCentimeters(HEIGHT_FT, HEIGHT_IN)).toBe(ROUND_TRIP_HEIGHT_CM);
    });
});
