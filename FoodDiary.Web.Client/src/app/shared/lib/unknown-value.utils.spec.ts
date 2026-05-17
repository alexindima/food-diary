import { describe, expect, it } from 'vitest';

import { getNumberProperty, getRecordProperty, getStringProperty, isRecord } from './unknown-value.utils';

describe('unknown value utils', () => {
    it('recognizes plain records only', () => {
        expect(isRecord({ value: 1 })).toBe(true);
        expect(isRecord(null)).toBe(false);
        expect(isRecord([])).toBe(false);
        expect(isRecord('value')).toBe(false);
    });

    it('reads properties from records', () => {
        const value = { name: 'Apple', amount: 2, enabled: true };

        expect(getRecordProperty(value, 'enabled')).toBe(true);
        expect(getStringProperty(value, 'name')).toBe('Apple');
        expect(getNumberProperty(value, 'amount')).toBe(2);
    });

    it('returns undefined for missing or mismatched properties', () => {
        expect(getRecordProperty(null, 'name')).toBeUndefined();
        expect(getStringProperty({ name: 1 }, 'name')).toBeUndefined();
        expect(getNumberProperty({ amount: '2' }, 'amount')).toBeUndefined();
    });
});
