import { describe, expect, it, vi } from 'vitest';

import { requireSdkFields, sdkDefinedFields, sdkEnum, sdkInstant, sdkMaybe, sdkNullableFields } from './sdk-response';

describe('SDK application boundaries', () => {
    it('preserves zero, false, fractional nutrition and optional null values', () => {
        const response = { amount: 0, completed: false, calories: 333.33, comment: null };
        expect(requireSdkFields(response, ['amount', 'completed', 'calories'])).toBe(response);
        expect(response.comment).toBeNull();
    });
    it('rejects a missing required field instead of silently inventing a nutrition total', () => {
        expect(() => requireSdkFields({ calories: undefined }, ['calories'])).toThrow('calories');
        expect(() => requireSdkFields({ calories: null }, ['calories'])).toThrow('calories');
    });
    it('uses only an explicit enum fallback', () => {
        expect(sdkEnum('future', ['known', 'unknown'], 'unknown')).toBe('unknown');
        expect(() => sdkEnum('future', ['known'])).toThrow();
    });
    it('preserves an instant across an offset and an already decoded Date', () => {
        expect(sdkInstant('2026-10-06T00:15:00+05:45').toISOString()).toBe('2026-10-05T18:30:00.000Z');
        const date = new Date('2026-10-05T18:30:00.000Z');
        expect(sdkInstant(date)).toBe(date);
    });
    it.each([
        { normalize: sdkDefinedFields, absent: undefined, convention: 'undefined' },
        { normalize: sdkNullableFields, absent: null, convention: 'null' },
    ])('writes selected fields as own data properties using $convention for absence', ({ normalize, absent }) => {
        const readLabel = vi.fn<() => string | null>().mockReturnValueOnce('spread value').mockReturnValue(null);
        const response = {
            get label(): string | null {
                return readLabel();
            },
            optional: undefined,
            untouched: null,
            amount: 0,
            completed: false,
        };

        const result = normalize(response, ['label', 'optional', 'amount', 'completed']);

        expect(result).toStrictEqual({ label: absent, optional: absent, untouched: null, amount: 0, completed: false });
        expect(readLabel).toHaveBeenCalledTimes(2);
        expect(Object.getOwnPropertyDescriptor(result, 'label')).toEqual({
            value: absent,
            enumerable: true,
            configurable: true,
            writable: true,
        });
        expect(Object.getOwnPropertyDescriptor(response, 'label')).toHaveProperty('get', expect.any(Function));
    });

    it('retains omitted, null, zero and false optional fields', () => {
        const mapper = (value: number | boolean): number | boolean => value;
        expect(sdkMaybe(undefined, mapper)).toBeUndefined();
        expect(sdkMaybe(null, mapper)).toBeNull();
        expect(sdkMaybe(0, mapper)).toBe(0);
        expect(sdkMaybe(false, mapper)).toBe(false);
        expect(sdkEnum(0, [0, 1] as const)).toBe(0);
        expect(() => sdkEnum(2, [0, 1] as const)).toThrow();
    });
});
