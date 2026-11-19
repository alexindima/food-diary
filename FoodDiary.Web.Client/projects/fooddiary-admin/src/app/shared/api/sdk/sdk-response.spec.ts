import { describe, expect, it, vi } from 'vitest';

import { requireSdkFields, sdkDefinedFields, sdkEnum, sdkInstant, sdkMaybe, sdkNullableFields, sdkOptional } from './sdk-response';

describe('admin SDK required fields', () => {
    it('keeps false, zero and empty arrays as valid required results', () => {
        const response = { enabled: false, total: 0, items: [] };
        expect(requireSdkFields(response, ['enabled', 'total', 'items'])).toBe(response);
    });

    it.each([undefined, null])('rejects a required field with value %s', total => {
        expect(() => requireSdkFields({ total }, ['total'])).toThrow('API response is missing required field: total.');
    });

    it('rejects an unknown enum unless the adapter supplies an explicit fallback', () => {
        const values = ['sent', 'pending'] as const;
        expect(sdkEnum('pending', values)).toBe('pending');
        expect(sdkEnum('accepted', values, 'pending')).toBe('pending');
        expect(() => sdkEnum('accepted', values)).toThrow('API response contains an unsupported enum value.');
    });
});

describe('admin SDK optional values', () => {
    it('creates an own field for a nullable or defined key omitted by the SDK', () => {
        const wire: { title?: string | null } = {};
        const nullable = sdkNullableFields(wire, ['title']);
        const defined = sdkDefinedFields(wire, ['title']);

        expect(nullable).toStrictEqual({ title: null });
        expect(defined).toStrictEqual({ title: undefined });
        expect(wire).toStrictEqual({});
    });

    it('normalizes missing values without overwriting supplied zero and false values or mutating the wire object', () => {
        const wire = Object.freeze({ omitted: undefined, explicitNull: null, views: 0, isEnabled: false, label: 'Sample' });
        const keys = ['omitted', 'explicitNull', 'views', 'isEnabled'] as const;
        const asDefined = sdkDefinedFields(wire, keys);
        const asNullable = sdkNullableFields(wire, keys);

        expect(asDefined).toStrictEqual({ omitted: undefined, explicitNull: undefined, views: 0, isEnabled: false, label: 'Sample' });
        expect(asNullable).toStrictEqual({ omitted: null, explicitNull: null, views: 0, isEnabled: false, label: 'Sample' });
        expect(wire.omitted).toBeUndefined();
        expect(wire.explicitNull).toBeNull();
    });

    it('keeps explicit null and missing distinct for optional nested responses', () => {
        const toId = vi.fn((item: { id: string }): string => item.id);
        expect(sdkMaybe(null, toId)).toBeNull();
        expect(sdkMaybe(undefined, toId)).toBeUndefined();
        expect(sdkOptional(null, toId)).toBeNull();
        expect(sdkOptional(undefined, toId)).toBeNull();
        expect(toId).not.toHaveBeenCalled();
        expect(sdkMaybe({ id: 'entry-1' }, toId)).toBe('entry-1');
    });

    it('decodes a wire instant and keeps an already decoded instance', () => {
        const decoded = sdkInstant('2026-10-06T14:25:00+04:00');
        expect(decoded.toISOString()).toBe('2026-10-06T10:25:00.000Z');
        expect(sdkInstant(decoded)).toBe(decoded);
    });
});
