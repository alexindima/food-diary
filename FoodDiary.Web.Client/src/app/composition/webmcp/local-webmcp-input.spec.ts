import { describe, expect, it, vi } from 'vitest';

import { MAXIMUM_CONSUMED_QUANTITY } from '../../shared/models/semantics/meal-quantity';
import { localModelContext } from './local-webmcp.types';
import { readDay, readDraft } from './local-webmcp-input';

const DST_SHIFTED_HOUR = 3;
const RECIPE_ID_OVER_LIMIT = 101;
const HALF_PORTION = 0.5;

const draft = {
    date: '2026-10-17',
    time: '12:30',
    mealType: 'LUNCH',
    comment: 'Pilot',
    items: [{ productName: 'Apple', amount: 150, unit: 'G' }],
};

describe('local WebMCP input boundary', () => {
    it.each(['2026-02-29', '2026-13-01', '2026-04-31', '2026-10-17T00:00:00Z', ''])('rejects invalid calendar date %s', value => {
        expect(() => readDay(value)).toThrow();
    });

    it('preserves leap days, local date, local time and fractional product quantities', () => {
        expect(readDay('2028-02-29')).toBe('2028-02-29');
        const input = { ...draft, time: '00:15', items: [{ productName: 'Apple', amount: 125.5, unit: 'G' }] };
        expect(readDraft(input)).toEqual(input);
    });

    it.each(['UTC', 'America/New_York', 'Asia/Kathmandu', 'Pacific/Auckland'])('preserves local midnight inputs in %s', timezone => {
        vi.stubEnv('TZ', timezone);
        try {
            expect(readDraft({ ...draft, time: '00:15' })).toMatchObject({ date: '2026-10-17', time: '00:15' });
            expect(readDay('2028-02-29')).toBe('2028-02-29');
        } finally {
            vi.unstubAllEnvs();
        }
    });

    it('rejects the nonexistent wall-clock time in the New York daylight-saving gap', () => {
        vi.stubEnv('TZ', 'America/New_York');
        try {
            expect(new Date('2026-03-08T02:30').getHours()).toBe(DST_SHIFTED_HOUR);
            expect(() => readDraft({ ...draft, date: '2026-03-08', time: '02:30' })).toThrow('does not exist');
        } finally {
            vi.unstubAllEnvs();
        }
    });

    it.each([
        { ...draft, time: '24:00' },
        { ...draft, mealType: 'INVALID' },
        { ...draft, save: true },
        { ...draft, items: [] },
        { ...draft, items: [{ ...draft.items[0], amount: 0 }] },
        { ...draft, items: [{ ...draft.items[0], amount: 0.001 }] },
        { ...draft, items: [{ ...draft.items[0], amount: Number.POSITIVE_INFINITY }] },
        { ...draft, items: [{ ...draft.items[0], amount: 1_000_001 }] },
    ])('rejects invalid or expanded draft arguments %j', input => {
        expect(() => readDraft(input)).toThrow();
    });

    it('requires development mode, loopback host and native registration support', () => {
        const context = { registerTool: vi.fn() };
        const document = (hostname: string, supported = true): Document =>
            ({ location: { hostname }, modelContext: supported ? context : undefined }) as unknown as Document;
        expect(localModelContext(document('localhost'), true)).toBe(context);
        expect(localModelContext(document('127.0.0.1'), true)).toBe(context);
        expect(localModelContext(document('[::1]'), true)).toBe(context);
        expect(localModelContext(document('localhost'), false)).toBeUndefined();
        expect(localModelContext(document('fooddiary.club'), true)).toBeUndefined();
        expect(localModelContext(document('localhost', false), true)).toBeUndefined();
    });
});

describe('local WebMCP recipe servings input', () => {
    it('preserves fractional recipe servings alongside a regular product item', () => {
        const input = { ...draft, items: [...draft.items, { recipeId: 'pancakes-for-two', servings: HALF_PORTION }] };
        expect(readDraft(input)).toEqual(input);
    });

    it('rejects recipe servings on a product row that has no recipe ID', () => {
        expect(() => readDraft({ ...draft, items: [{ ...draft.items[0], servings: 1 }] })).toThrow('Servings require recipeId.');
    });

    it.each([
        { recipeId: 'existing-recipe', servings: 1, productName: 'Apple' },
        { recipeId: 1, servings: 1 },
        { recipeId: 'existing-recipe', servings: '1' },
        { recipeId: '', servings: 1 },
        { recipeId: 'r'.repeat(RECIPE_ID_OVER_LIMIT), servings: 1 },
        { recipeId: 'existing-recipe', servings: Number.POSITIVE_INFINITY },
        { recipeId: 'existing-recipe', servings: 0 },
        { recipeId: 'existing-recipe', servings: MAXIMUM_CONSUMED_QUANTITY + 1 },
    ])('rejects recipe input outside the published ID and serving contract (%j)', item => {
        expect(() => readDraft({ ...draft, items: [item] })).toThrow('exact recipe ID and a bounded serving quantity');
    });
});
